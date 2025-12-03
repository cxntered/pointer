using pointer.Core;
using pointer.Core.Readers;
using pointer.Core.Utils;

if (args.Length == 0)
{
    Console.WriteLine("pointer! - point your osu!lazer files back to stable!");
    Console.WriteLine();
    Console.WriteLine("Usage: pointer [options] [lazer-path] [stable-path]");
    Console.WriteLine();
    Console.WriteLine("Arguments:");
    Console.WriteLine("  lazer-path     The path to osu!lazer's install directory, optional");
    Console.WriteLine("  stable-path    The path to osu!stable's install directory, optional");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  -b, --beatmaps     Convert beatmaps from osu!lazer to osu!stable");
    Console.WriteLine("  -c, --collections  Convert collections from osu!lazer to osu!stable");
    Console.WriteLine("  -s, --skins        Convert skins from osu!lazer to osu!stable");
    Console.WriteLine("  -r, --scores       Convert scores from osu!lazer to osu!stable");
    return 0;
}

try
{
    var arguments = args.Where(a => !a.StartsWith("-")).ToArray();
    string lazerPath = arguments.Length > 0 ? arguments[0] : PathResolver.GetDefaultLazerPath();
    string stablePath = arguments.Length > 1 ? arguments[1] : PathResolver.GetDefaultStablePath();
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

    if (FileLinker.IsHardLinkSupported(lazerPath, stablePath))
    {
        Console.WriteLine("Hard linking is supported. Hard linked files will not use extra disk space.");
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Hard linking is not supported (folders may be on different drives or file system doesn't support it).");
        Console.WriteLine("Files will be copied instead, which will use more disk space.");
        Console.ResetColor();
    }
    Console.WriteLine();

    if (args.Contains("-b") || args.Contains("--beatmaps"))
    {
        Console.WriteLine("Converting beatmaps...");
        manager.ConvertBeatmaps();
    }

    if (args.Contains("-c") || args.Contains("--collections"))
    {
        Console.WriteLine("Converting collections...");
        manager.ConvertCollections();
    }

    if (args.Contains("-s") || args.Contains("--skins"))
    {
        Console.WriteLine("Converting skins...");
        manager.ConvertSkins();
    }

    if (args.Contains("-r") || args.Contains("--scores"))
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