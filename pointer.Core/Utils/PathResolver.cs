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
        if (OperatingSystem.IsWindows())
        {
            // %LocalAppData%\osu!
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "osu!");
        }
        else if (OperatingSystem.IsMacOS())
        {
            // /Applications/osu!.app/Contents/Resources/drive_c/Program Files/osu!
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "osu!.app",
                "Contents",
                "Resources",
                "drive_c",
                "Program Files",
                "osu!"
            );
        }
        else
        {
            // ~/.local/share/osu-stable
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "osu-stable");
        }
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
