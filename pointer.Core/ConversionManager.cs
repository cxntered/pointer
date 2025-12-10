namespace pointer.Core;

using pointer.Core.Models;
using pointer.Core.Readers;
using pointer.Core.Utils;

public class ConversionManager(LazerDatabaseReader lazer, StableDatabaseReader stable, string lazerPath, string stablePath, string stableSongsPath)
{
    private const int STABLE_VERSION = 20251128;

    public IEnumerable<BeatmapSetInfo> GetBeatmapSetsToConvert()
    {
        var stableHashes = stable.GetBeatmaps()
            .Select(b => b.Hash)
            .ToHashSet();

        foreach (var beatmapSet in lazer.GetBeatmapSets())
        {
            if (beatmapSet.Protected) continue;

            var toConvert = beatmapSet.Beatmaps
                .Where(b => !stableHashes.Contains(b.Hash))
                .ToList();
            if (toConvert.Count == 0) continue;

            yield return beatmapSet;
        }
    }

    public void ConvertBeatmaps(IEnumerable<BeatmapSetInfo> beatmapSets)
    {
        foreach (var beatmapSet in beatmapSets)
        {
            Console.WriteLine($"Converting BeatmapSet: {beatmapSet.Artist} - {beatmapSet.Title} ({beatmapSet.Creator}) [{beatmapSet.OnlineID}]");

            string beatmapId = beatmapSet.OnlineID > 0 ? $"{beatmapSet.OnlineID} " : "";
            string folderName = SanitizePath($"{beatmapId}{beatmapSet.Artist} - {beatmapSet.Title}");

            foreach (var file in beatmapSet.Files)
            {
                string sourceFile = Path.Combine(Path.Combine(lazerPath, "files"), file.Hash[..1], file.Hash[..2], file.Hash);
                string destFile = Path.Combine(stableSongsPath, folderName, file.Filename);
                try
                {
                    FileLinker.LinkOrCopy(sourceFile, destFile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Error linking/copying file '{file.Filename}': {ex.Message}");
                }
            }
        }
    }

    public IEnumerable<Score> GetScoresToConvert()
    {
        var stableScores = stable.GetScores().ToList();

        foreach (var score in lazer.GetScores())
        {
            if (score.IsLegacyScore) continue;

            var stableScore = ConvertToStableScore(score);
            if (stableScore == null) continue;

            bool exists = stableScores.Any(s =>
                s.BeatmapHash == score.BeatmapMD5Hash &&
                (s.ReplayHash == stableScore.ReplayHash ||
                (s.OnlineScoreId > 0 && s.OnlineScoreId == stableScore.OnlineScoreId)));

            if (!exists)
                yield return score with { StableScore = stableScore };
        }
    }

    public void ConvertScores(IEnumerable<Score> scores)
    {
        // link lazer replays to stable replays folder
        int count = 0;
        foreach (var score in scores)
        {
            count++;
            var file = score.Files.FirstOrDefault()!; // assume first (and only) file is the replay
            string sourceFile = Path.Combine(Path.Combine(lazerPath, "files"), file.Hash[..1], file.Hash[..2], file.Hash);
            string destFile = Path.Combine(stablePath, "Data", "r", $"{score.BeatmapMD5Hash}-{score.Date.ToFileTime()}.osr");
            try
            {
                FileLinker.LinkOrCopy(sourceFile, destFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error linking/copying replay file: {ex.Message}");
            }
        }

        // write merged scores to scores.db
        var mergedScores = stable.GetScores().Concat(scores.Select(s => s.StableScore!));
        var groupedScores = mergedScores.GroupBy(s => s.BeatmapHash).ToDictionary(g => g.Key, g => g.ToList());

        string scoresDbPath = Path.Combine(stablePath, "scores.db");
        string backupPath = Path.Combine(stablePath, "scores.db.bak");

        if (System.IO.File.Exists(scoresDbPath))
        {
            System.IO.File.Copy(scoresDbPath, backupPath, overwrite: true);
            Console.WriteLine($"Backed up scores.db to scores.db.bak");
        }

        using var stream = System.IO.File.Create(scoresDbPath);
        using var writer = new BinaryWriter(stream);

        writer.Write(STABLE_VERSION);
        writer.Write(groupedScores.Count);

        foreach (var (beatmapHash, beatmapScores) in groupedScores)
        {
            WriteString(writer, beatmapHash);
            writer.Write(beatmapScores.Count);

            foreach (var score in beatmapScores.OrderByDescending(s => s.ReplayScore))
            {
                writer.Write(score.GameMode);
                writer.Write(score.Version);
                WriteString(writer, score.BeatmapHash);
                WriteString(writer, score.PlayerName);
                WriteString(writer, score.ReplayHash);
                writer.Write(score.Count300);
                writer.Write(score.Count100);
                writer.Write(score.Count50);
                writer.Write(score.CountGeki);
                writer.Write(score.CountKatu);
                writer.Write(score.CountMiss);
                writer.Write(score.ReplayScore);
                writer.Write(score.MaxCombo);
                writer.Write(score.PerfectCombo);
                writer.Write((int)score.Mods);
                WriteString(writer, score.HealthGraph);
                writer.Write(score.Timestamp);
                writer.Write(score.CompressedReplayLength);
                writer.Write(score.OnlineScoreId);

                if (score.AdditionalModInfo.HasValue)
                {
                    writer.Write(score.AdditionalModInfo.Value);
                }
            }
        }

        Console.WriteLine($"Wrote scores for {groupedScores.Count} beatmaps to scores.db ({count} new scores converted)");
    }

    public IEnumerable<Skin> GetSkinsToConvert()
    {
        var stableSkins = Directory.Exists(Path.Combine(stablePath, "Skins"))
            ? Directory.GetDirectories(Path.Combine(stablePath, "Skins"))
                .Select(dir => Path.GetFileName(dir))
                .ToHashSet()
            : new HashSet<string>();

        foreach (var skin in lazer.GetSkins())
        {
            if (skin.InstantiationInfo != "osu.Game.Skinning.LegacySkin, osu.Game") continue; // skip non-legacy skins

            string? iniName = GetSkinNameFromIni(skin);
            string skinName = ExtractSkinName(skin.Name, iniName);

            if (!stableSkins.Contains(skinName))
                yield return skin with { Name = skinName, IniName = iniName };
        }
    }

    public void ConvertSkins(IEnumerable<Skin> skins)
    {
        foreach (var skin in skins)
        {
            Console.WriteLine($"Converting Skin: {skin.Name}");

            string skinDir = Path.Combine(stablePath, "Skins", skin.Name);
            Directory.CreateDirectory(skinDir);

            foreach (var file in skin.Files)
            {
                string sourceFile = Path.Combine(Path.Combine(lazerPath, "files"), file.Hash[..1], file.Hash[..2], file.Hash);
                string destFile = Path.Combine(skinDir, file.Filename);
                try
                {
                    FileLinker.LinkOrCopy(sourceFile, destFile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Error linking/copying file '{file.Filename}': {ex.Message}");
                }
            }
        }
    }

    public IEnumerable<BeatmapCollection> GetCollectionsToConvert()
    {
        var stableCollectionHashes = stable.GetCollections()
            .SelectMany(c => c.Hashes)
            .ToHashSet();

        foreach (var collection in lazer.GetCollections())
        {
            if (collection.Hashes.Any(hash => !stableCollectionHashes.Contains(hash)))
                yield return collection;
        }
    }

    public void ConvertCollections(IEnumerable<BeatmapCollection> collections)
    {
        var mergedCollections = stable.GetCollections()
            .Concat(collections)
            .GroupBy(c => c.Name)
            .Select(g => new BeatmapCollection(
                Name: g.Key,
                Hashes: g.SelectMany(c => c.Hashes).Distinct().ToList()
            ))
            .ToList();

        string collectionDbPath = Path.Combine(stablePath, "collection.db");
        string backupPath = Path.Combine(stablePath, "collection.db.bak");

        if (System.IO.File.Exists(collectionDbPath))
        {
            System.IO.File.Copy(collectionDbPath, backupPath, overwrite: true);
            Console.WriteLine($"Backed up collection.db to collection.db.bak");
        }

        using var stream = System.IO.File.Create(collectionDbPath);
        using var writer = new BinaryWriter(stream);

        writer.Write(STABLE_VERSION);
        writer.Write(mergedCollections.Count);

        foreach (var collection in mergedCollections)
        {
            WriteString(writer, collection.Name);
            writer.Write(collection.Hashes.Count);

            foreach (var hash in collection.Hashes)
            {
                WriteString(writer, hash);
            }
        }

        Console.WriteLine($"Wrote {mergedCollections.Count} collections to collection.db");
    }

    private static string SanitizePath(string path)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var c in invalidChars)
        {
            path = path.Replace(c, '_');
        }
        path = path.Replace(".", "");

        return path;
    }

    private static void WriteString(BinaryWriter w, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            w.Write((byte)0x00);
            return;
        }

        w.Write((byte)0x0B);
        byte[] data = System.Text.Encoding.UTF8.GetBytes(value);
        WriteULEB128(w, data.Length);
        w.Write(data);
    }

    private static void WriteULEB128(BinaryWriter w, int value)
    {
        do
        {
            byte b = (byte)(value & 0x7F);
            value >>= 7;
            if (value != 0)
                b |= 0x80;
            w.Write(b);
        } while (value != 0);
    }

    private string? GetSkinNameFromIni(Skin skin)
    {
        var skinIni = skin.Files.FirstOrDefault(f => f.Filename.Equals("skin.ini", StringComparison.OrdinalIgnoreCase));
        if (skinIni == null) return null;

        string skinIniPath = Path.Combine(Path.Combine(lazerPath, "files"), skinIni.Hash[..1], skinIni.Hash[..2], skinIni.Hash);
        if (!System.IO.File.Exists(skinIniPath)) return null;

        bool inGeneralSection = false;
        foreach (var line in System.IO.File.ReadLines(skinIniPath))
        {
            var trimmed = line.Trim();

            if (trimmed.Equals("[General]"))
            {
                inGeneralSection = true;
                continue;
            }

            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                inGeneralSection = false;
                continue;
            }

            if (inGeneralSection && trimmed.StartsWith("Name:"))
            {
                return trimmed["Name:".Length..].Trim();
            }
        }

        return null;
    }

    private static string ExtractSkinName(string skinName, string? iniName)
    {
        if (iniName == null) return skinName;

        if (skinName == iniName) return iniName;

        // in osu!lazer, if the skin name differs from the ini name, it's formatted as "{iniName} [{skinName}]"
        // to get the actual skin name used for the folder, we need to extract it here
        string prefix = $"{iniName} [";
        if (skinName.StartsWith(prefix) && skinName.EndsWith(']'))
        {
            int startIndex = prefix.Length;
            int length = skinName.Length - startIndex - 1;
            return skinName.Substring(startIndex, length);
        }

        return skinName;
    }

    private static StableScore? ConvertToStableScore(Score score)
    {
        BitwiseMods bitwiseMods = 0;
        foreach (Mod mod in score.Mods)
        {
            string enumName = char.IsDigit(mod.Acronym[0]) ? "_" + mod.Acronym : mod.Acronym;

            if (Enum.TryParse(enumName, ignoreCase: true, out BitwiseMods bitwiseMod))
            {
                bitwiseMods |= bitwiseMod;
            }
            else
            {
                return null; // score has lazer only mods, skip conversion
            }
        }
        bitwiseMods |= BitwiseMods.SV2; // set all converted scores to scorev2

        bool isPerfectCombo = score.Statistics.Miss == 0 &&
                           score.Statistics.LargeTickMiss == 0 &&
                           score.Statistics.SmallTickMiss == 0;

        return new StableScore(
            GameMode: (byte)score.Ruleset.ID,
            Version: STABLE_VERSION,
            BeatmapHash: score.BeatmapMD5Hash,
            PlayerName: score.User.Username,
            ReplayHash: score.MD5Hash,
            Count300: (short)score.Statistics.Great,
            Count100: (short)score.Statistics.Ok,
            Count50: (short)score.Statistics.Meh,
            CountGeki: (short)score.Statistics.Perfect,
            CountKatu: (short)score.Statistics.Good,
            CountMiss: (short)score.Statistics.Miss,
            ReplayScore: score.TotalScore,
            MaxCombo: (short)score.MaxCombo,
            PerfectCombo: isPerfectCombo,
            Mods: bitwiseMods,
            HealthGraph: string.Empty,
            Timestamp: score.Date.UtcTicks,
            CompressedReplayLength: -1,
            OnlineScoreId: score.ID,
            AdditionalModInfo: null
        );
    }
}