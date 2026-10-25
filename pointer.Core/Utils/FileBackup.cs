namespace pointer.Core.Utils;

public static class FileBackup
{
    public static string? Create(string path)
    {
        if (!File.Exists(path))
            return null;

        string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
        string backupPath = $"{path}.{timestamp}.bak";

        int suffix = 1;
        while (File.Exists(backupPath))
            backupPath = $"{path}.{timestamp}-{suffix++}.bak";

        File.Copy(path, backupPath);
        return backupPath;
    }
}
