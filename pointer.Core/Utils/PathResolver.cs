namespace pointer.Core.Utils;

public static class PathResolver
{
    public static string GetDefaultLazerPath()
    {
        string basePath;
        if (OperatingSystem.IsWindows())
        {
            // %AppData%\osu
            basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }
        else
        {
            // ~/Library/Application Support/osu or ~/.local/share/osu
            basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }
        return Path.Combine(basePath, "osu");
    }

    public static string GetDefaultStablePath()
    {
        string basePath;
        if (OperatingSystem.IsWindows())
        {
            // %LocalAppData%\osu!
            basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }
        else if (OperatingSystem.IsMacOS())
        {
            // /Applications/osu!.app/Contents/Resources/drive_c/Program Files/osu!
            basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "osu!.app",
                "Contents",
                "Resources",
                "drive_c",
                "Program Files"
            );
        }
        else
        {
            // ~/.local/share/osu-wine/osu!
            basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "osu-wine");
        }
        return Path.Combine(basePath, "osu!");
    }

    public static string GetStableSongsPath(string basePath)
    {
        string configPath = Path.Combine(basePath, "osu!." + Environment.UserName + ".cfg");

        if (File.Exists(configPath))
        {
            foreach (var line in File.ReadLines(configPath))
            {
                if (line.StartsWith("BeatmapDirectory", StringComparison.OrdinalIgnoreCase))
                {
                    string? dir = line.Split('=').LastOrDefault()?.Trim();
                    if (Path.IsPathRooted(dir))
                        return dir;
                    else if (!string.IsNullOrEmpty(dir))
                        return Path.GetFullPath(Path.Combine(basePath, dir));
                }
            }
        }

        return Path.Combine(basePath, "Songs");
    }
}
