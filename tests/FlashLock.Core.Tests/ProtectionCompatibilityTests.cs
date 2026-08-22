using FlashLock.Core;

namespace FlashLock.Core.Tests;

public sealed class ProtectionCompatibilityTests
{
    [Fact]
    public void NonNtfsDrive_IsRejected()
    {
        var drive = new PortableDriveInfo(
            "Z:\\",
            "exFAT",
            DriveType.Removable,
            "TEST",
            1000,
            500,
            "01020304");

        var result = ProtectionCompatibility.Evaluate(drive);
        Assert.Equal(ProtectionCompatibilityStatus.RequiresNtfs, result.Status);
        Assert.False(result.CanProtect);
    }
}
