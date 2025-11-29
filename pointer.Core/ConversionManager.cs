namespace pointer.Core;

using pointer.Core.Readers;

public class ConversionManager(LazerDatabaseReader lazer, StableDatabaseReader stable, string lazerFilesPath, string stableSongsPath)
{
    private readonly LazerDatabaseReader lazer = lazer;
    private readonly StableDatabaseReader stable = stable;
    private readonly string lazerFilesPath = lazerFilesPath;
    private readonly string stableSongsPath = stableSongsPath;

    public void Convert()
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
                string sourceFile = Path.Combine(lazerFilesPath, file.Hash[..1], file.Hash[..2], file.Hash);
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
}