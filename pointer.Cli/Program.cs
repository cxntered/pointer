using pointer.Core;
using pointer.Core.Readers;
using pointer.Core.Utils;

if (args.Length == 0)
{
    Console.WriteLine("Usage: pointer [--beatmaps] [--collections] [--skins] [--scores]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --beatmaps     Convert beatmaps from osu!lazer to osu!stable");
    Console.WriteLine("  --collections  Convert collections from osu!lazer to osu!stable");
    Console.WriteLine("  --skins        Convert skins from osu!lazer to osu!stable");
    Console.WriteLine("  --scores       Convert scores from osu!lazer to osu!stable");
    return 0;
}

try
{
    string lazerPath = PathResolver.GetDefaultLazerPath();
    string stablePath = PathResolver.GetDefaultStablePath();
    string stableSongsPath = PathResolver.GetStableSongsPath(stablePath);

    var lazerReader = new LazerDatabaseReader(lazerPath);
    var stableReader = new StableDatabaseReader(stablePath);

    var manager = new ConversionManager(
        lazerReader,
        stableReader,
        lazerPath,
        stablePath,
        stableSongsPath
    );

    if (args.Contains("--beatmaps"))
    {
        Console.WriteLine("Converting beatmaps...");
        manager.ConvertBeatmaps();
    }

    if (args.Contains("--collections"))
    {
        Console.WriteLine("Converting collections...");
        manager.ConvertCollections();
    }

    if (args.Contains("--skins"))
    {
        Console.WriteLine("Converting skins...");
        manager.ConvertSkins();
    }

    if (args.Contains("--scores"))
    {
        Console.WriteLine("Converting scores...");
        manager.ConvertScores();
    }

    Console.WriteLine("Conversion completed successfully.");
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Fatal error: {ex.Message}");
    Console.ResetColor();
    return 1;
}

return 0;