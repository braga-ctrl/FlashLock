namespace FlashLock.Core;

public sealed class PortableDriveLocator
{
    public PortableDriveInfo LocateFromExecutable(string? executablePath = null)
    {
        var path = executablePath ?? Environment.ProcessPath
            ?? throw new InvalidOperationException("Unable to determine the executable path.");

        var root = Path.GetPathRoot(Path.GetFullPath(path));
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new InvalidOperationException("Unable to determine the drive containing FlashLock.");
        }

        var drive = new DriveInfo(root);
        if (!drive.IsReady)
        {
            throw new IOException($"Drive {root} is not ready.");
        }

        return new PortableDriveInfo(
            drive.RootDirectory.FullName,
            drive.DriveFormat,
            drive.DriveType,
            string.IsNullOrWhiteSpace(drive.VolumeLabel) ? null : drive.VolumeLabel,
            drive.TotalSize,
            drive.AvailableFreeSpace,
            VolumeIdentityProvider.GetSerialNumber(root));
    }
}
