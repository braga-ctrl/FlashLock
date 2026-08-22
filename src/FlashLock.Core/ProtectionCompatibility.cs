namespace FlashLock.Core;

public enum ProtectionCompatibilityStatus
{
    Supported,
    RequiresNtfs,
    SystemDrive,
    NotReady
}

public sealed record ProtectionCompatibilityResult(ProtectionCompatibilityStatus Status, string Message)
{
    public bool CanProtect => Status == ProtectionCompatibilityStatus.Supported;
}

public static class ProtectionCompatibility
{
    public static ProtectionCompatibilityResult Evaluate(PortableDriveInfo drive)
    {
        var systemRoot = Path.GetPathRoot(Environment.SystemDirectory);
        if (string.Equals(drive.RootPath, systemRoot, StringComparison.OrdinalIgnoreCase))
        {
            return new(
                ProtectionCompatibilityStatus.SystemDrive,
                "FlashLock will never apply protection to the Windows system drive.");
        }

        if (!drive.SupportsPortableAclProtection)
        {
            return new(
                ProtectionCompatibilityStatus.RequiresNtfs,
                $"Portable protection requires NTFS. This drive is {drive.FileSystem}.");
        }

        return new(
            ProtectionCompatibilityStatus.Supported,
            "NTFS portable protection is available for this volume.");
    }
}
