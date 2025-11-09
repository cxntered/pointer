using pointer.Core.Readers;

if (args.Contains("--stable"))
{
    string stablePath = ParseDbPath(args, "--stable", GetDefaultStablePath());
    if (!File.Exists(stablePath))
    {
        Console.WriteLine($"Error: osu!.db not found at {stablePath}");
    }
    else
    {
        Console.WriteLine($"=== osu!stable beatmaps from {stablePath} ===\n");
        var stableReader = new StableDatabaseReader(stablePath);
        int count = 0;
        foreach (var bm in stableReader.GetBeatmaps())
        {
            Console.WriteLine($"[{++count}] {bm.Artist} - {bm.Title} ({bm.Creator}) [{bm.Difficulty}]");
            Console.WriteLine($"    Folder: {bm.FolderName}");
            Console.WriteLine($"    Hash:   {bm.Hash}\n");
        }
        Console.WriteLine($"Loaded {count} stable beatmaps.");
    }
}

if (args.Contains("--lazer"))
{
    string lazerPath = ParseDbPath(args, "--lazer", GetDefaultLazerPath());
    if (!File.Exists(lazerPath))
    {
        Console.WriteLine($"Error: client.realm not found at {lazerPath}");
    }
    else
    {
        Console.WriteLine($"=== osu!lazer beatmaps from {lazerPath} ===\n");
        var lazerReader = new LazerDatabaseReader(lazerPath);
        int count = 0;
        foreach (var bm in lazerReader.GetBeatmaps())
        {
            Console.WriteLine($"[{++count}] {bm.Artist} - {bm.Title} ({bm.Creator}) [{bm.Difficulty}]");
            Console.WriteLine($"    Hash: {bm.Hash}\n");
        }
        Console.WriteLine($"Loaded {count} lazer beatmaps.");
    }
}

static string ParseDbPath(string[] args, string flag, string defaultPath)
{
    int idx = Array.IndexOf(args, flag);

    if (idx == -1) return defaultPath;
    if (idx + 1 >= args.Length || args[idx + 1].StartsWith("--"))
        return defaultPath;

    return args[idx + 1];
}

static string GetDefaultStablePath()
{
    if (OperatingSystem.IsWindows())
    {
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(local, "osu!", "osu!.db");
    }
    string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    return Path.Combine(home, ".local", "share", "osu", "osu!.db");
}

static string GetDefaultLazerPath()
{
    if (OperatingSystem.IsWindows())
    {
        string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(roaming, "osu", "client.realm");
    }
    string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    return Path.Combine(home, ".osu", "client.realm");
}
