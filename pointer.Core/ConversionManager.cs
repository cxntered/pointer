namespace pointer.Core;

using pointer.Core.Models;
using pointer.Core.Readers;

public class ConversionManager(LazerDatabaseReader lazer, StableDatabaseReader stable, string lazerPath, string stablePath, string stableSongsPath)
{
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

        writer.Write(20251128); // client version
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
}