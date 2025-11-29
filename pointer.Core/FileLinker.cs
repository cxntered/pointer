namespace pointer.Core;

using System.Runtime.InteropServices;

public static class FileLinker
{
    public static bool LinkOrCopy(string sourcePath, string destinationPath)
    {
        var destDir = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(destDir))
            Directory.CreateDirectory(destDir);

        if (File.Exists(destinationPath))
            File.Delete(destinationPath);

        // attempt hard linking first
        if (TryCreateHardLink(sourcePath, destinationPath))
            return true;

        // hard link failed, fallback to copying
        File.Copy(sourcePath, destinationPath);
        return false;
    }

    private static bool TryCreateHardLink(string sourcePath, string destinationPath)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return CreateHardLink(destinationPath, sourcePath, IntPtr.Zero);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return UnixLink(sourcePath, destinationPath) == 0;
            }
        }
        catch
        {
            // hard link not supported or failed
        }

        return false;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

    [DllImport("libc", EntryPoint = "link", SetLastError = true)]
    private static extern int UnixLink(string oldpath, string newpath);
}
