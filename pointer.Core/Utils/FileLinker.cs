namespace pointer.Core.Utils;

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

    public static bool TryCreateHardLink(string sourcePath, string destinationPath)
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

    public static bool IsHardLinkSupported(string sourcePath, string destinationPath)
    {
        string testSourceFile = Path.Combine(sourcePath, $".hard_link_test");
        string testDestFile = Path.Combine(destinationPath, $".hard_link_test");

        try
        {
            Directory.CreateDirectory(sourcePath);
            Directory.CreateDirectory(destinationPath);
            File.WriteAllText(testSourceFile, "created by pointer!");

            bool success = TryCreateHardLink(testSourceFile, testDestFile);

            if (File.Exists(testSourceFile))
                File.Delete(testSourceFile);
            if (File.Exists(testDestFile))
                File.Delete(testDestFile);

            return success;
        }
        catch
        {
            try
            {
                if (File.Exists(testSourceFile))
                    File.Delete(testSourceFile);
                if (File.Exists(testDestFile))
                    File.Delete(testDestFile);
            }
            catch { }
            return false;
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

    [DllImport("libc", EntryPoint = "link", SetLastError = true)]
    private static extern int UnixLink(string oldpath, string newpath);
}
