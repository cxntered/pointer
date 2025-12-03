namespace pointer.Core;

using pointer.Core.Models;
using pointer.Core.Readers;
using pointer.Core.Utils;

public class ConversionManager(LazerDatabaseReader lazer, StableDatabaseReader stable, string lazerPath, string stablePath, string stableSongsPath)
{
    private const int STABLE_VERSION = 20251128;

    public void ConvertBeatmaps()
    {
        var stableHashes = stable.GetBeatmaps()
            .Select(b => b.Hash)
            .ToHashSet();

        foreach (var beatmapSet in lazer.GetBeatmapSets())
        {
            // skip protected beatmap sets (intro sequences)
            if (beatmapSet.Protected) continue;

            var toConvert = beatmapSet.Beatmaps
                .Where(b => !stableHashes.Contains(b.Hash))
                .ToList();
            if (toConvert.Count == 0) continue;

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

    public void ConvertCollections()
    {
        var mergedCollections = stable.GetCollections()
            .Concat(lazer.GetCollections())
            .GroupBy(c => c.Name)
            .ToDictionary(
                g => g.Key,
                g => new HashSet<string>(g.SelectMany(c => c.Hashes))
            );

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

        foreach (var (name, hashes) in mergedCollections)
        {
            WriteString(writer, name);
            writer.Write(hashes.Count);

            foreach (var hash in hashes)
            {
                WriteString(writer, hash);
            }
        }

        Console.WriteLine($"Wrote {mergedCollections.Count} collections to collection.db");
    }

    public void ConvertSkins()
    {
        var stableSkins = Directory.GetDirectories(Path.Combine(stablePath, "Skins"))
            .Select(dir => Path.GetFileName(dir))
            .ToHashSet();

        foreach (var skin in lazer.GetSkins())
        {
            if (skin.InstantiationInfo != "osu.Game.Skinning.LegacySkin, osu.Game") continue; // skip non-legacy skins

            string? iniName = GetSkinNameFromIni(skin);
            string skinName = ExtractSkinName(skin.Name, iniName);

            if (stableSkins.Contains(skinName)) continue;

            Console.WriteLine($"Converting Skin: {skinName}");

            string skinDir = Path.Combine(stablePath, "Skins", skinName);
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

    public void ConvertScores()
    {
        var stableScores = stable.GetScores();

        int convertedCount = 0;
        foreach (var score in lazer.GetScores())
        {
            if (score.IsLegacyScore) continue;

            var stableScore = ConvertToStableScore(score);
            if (stableScore == null) continue;

            if (!stableScores.ContainsKey(score.BeatmapMD5Hash))
            {
                stableScores[score.BeatmapMD5Hash] = new List<StableScore>();
            }

            bool exists = stableScores[score.BeatmapMD5Hash].Any(s =>
                s.ReplayHash == stableScore.ReplayHash ||
                (s.OnlineScoreId > 0 && s.OnlineScoreId == stableScore.OnlineScoreId));

            if (!exists)
            {
                stableScores[score.BeatmapMD5Hash].Add(stableScore);
                convertedCount++;

                // link lazer replays to stable replays folder
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
        }

        // write merged scores to scores.db
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
        writer.Write(stableScores.Count);

        foreach (var (beatmapHash, scores) in stableScores)
        {
            WriteString(writer, beatmapHash);
            writer.Write(scores.Count);

            foreach (var score in scores.OrderByDescending(s => s.ReplayScore))
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
                writer.Write(score.Mods);
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

        Console.WriteLine($"Wrote scores for {stableScores.Count} beatmaps to scores.db ({convertedCount} new scores converted)");
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
        int bitwiseMods = 0;
        foreach (Mod mod in score.Mods)
        {
            string enumName = char.IsDigit(mod.Acronym[0]) ? "_" + mod.Acronym : mod.Acronym;

            if (Enum.TryParse(enumName, true, out BitwiseMods bitwiseMod))
            {
                bitwiseMods |= (int)bitwiseMod;
            }
            else
            {
                return null; // score has lazer only mods, skip conversion
            }
        }
        bitwiseMods |= (int)BitwiseMods.SV2; // set all converted scores to scorev2

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