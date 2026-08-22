using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace FlashLock.Core;

public static class VolumeIdentityProvider
{
    public static string GetSerialNumber(string rootPath)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("FlashLock volume identity requires Windows.");
        }

        var root = Path.GetPathRoot(Path.GetFullPath(rootPath))
            ?? throw new ArgumentException("A drive root is required.", nameof(rootPath));

        var volumeName = new StringBuilder(261);
        var fileSystemName = new StringBuilder(261);
        if (!GetVolumeInformation(
                root,
                volumeName,
                volumeName.Capacity,
                out var serial,
                out _,
                out _,
                fileSystemName,
                fileSystemName.Capacity))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"Unable to read volume identity for {root}.");
        }

        return serial.ToString("X8");
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetVolumeInformation(
        string lpRootPathName,
        StringBuilder lpVolumeNameBuffer,
        int nVolumeNameSize,
        out uint lpVolumeSerialNumber,
        out uint lpMaximumComponentLength,
        out uint lpFileSystemFlags,
        StringBuilder lpFileSystemNameBuffer,
        int nFileSystemNameSize);
}
