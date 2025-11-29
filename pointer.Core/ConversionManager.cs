namespace pointer.Core;

using pointer.Core.Readers;

public class ConversionManager(LazerDatabaseReader lazer, StableDatabaseReader stable)
{
    private readonly LazerDatabaseReader lazer = lazer;
    private readonly StableDatabaseReader stable = stable;

    public void Convert()
    {
        var stableHashes = stable.GetBeatmaps()
            .Select(b => b.Hash)
            .ToHashSet();

        foreach (var beatmapSet in lazer.GetBeatmapSets())
        {
            var toConvert = beatmapSet.Beatmaps
                .Where(b => !stableHashes.Contains(b.Hash))
                .ToList();

            if (toConvert.Count == 0)
                continue;

            // TODO: implement hard linking/copying

            Console.WriteLine($"Converting BeatmapSet: {beatmapSet.Artist} - {beatmapSet.Title} ({beatmapSet.Creator}) [{beatmapSet.OnlineID}]");
            foreach (var beatmap in toConvert)
            {
                Console.WriteLine($" - {beatmap.Artist} - {beatmap.Title} [{beatmap.Difficulty}]");
            }
        }
    }
}