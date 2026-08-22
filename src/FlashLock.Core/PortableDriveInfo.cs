namespace FlashLock.Core;

public sealed record PortableDriveInfo(
    string RootPath,
    string FileSystem,
    DriveType DriveType,
    string? VolumeLabel,
    long TotalSize,
    long AvailableFreeSpace,
    string VolumeSerialNumber)
{
    public bool SupportsPortableAclProtection =>
        string.Equals(FileSystem, "NTFS", StringComparison.OrdinalIgnoreCase);
}
