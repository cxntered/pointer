using pointer.Core;
using pointer.Core.Readers;

var lazerReader = new LazerDatabaseReader(GetDefaultLazerPath());
var stableReader = new StableDatabaseReader(GetDefaultStablePath());

var manager = new ConversionManager(lazerReader, stableReader);
manager.Convert();

static string GetDefaultStablePath()
{
    if (OperatingSystem.IsWindows())
    {
        // %LocalAppData%\osu!
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "osu!");
    }
    else if (OperatingSystem.IsMacOS())
    {
        // ~/Applications/osu!.app/Contents/Resources/drive_c/Program Files/osu!
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

static string GetDefaultLazerPath()
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
