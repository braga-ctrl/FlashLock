using System.Security.AccessControl;
using FlashLock.Core;

namespace FlashLock.Core.Tests;

public sealed class ProtectionVerifierTests
{
    [Fact]
    public void ReadAndExecute_DoesNotCountAsForbiddenWriteAccess()
    {
        Assert.False(ProtectionVerifier.ContainsForbiddenNormalUserRights(FileSystemRights.ReadAndExecute));
    }

    [Theory]
    [InlineData(FileSystemRights.WriteData)]
    [InlineData(FileSystemRights.AppendData)]
    [InlineData(FileSystemRights.WriteExtendedAttributes)]
    [InlineData(FileSystemRights.WriteAttributes)]
    [InlineData(FileSystemRights.Delete)]
    [InlineData(FileSystemRights.DeleteSubdirectoriesAndFiles)]
    [InlineData(FileSystemRights.ChangePermissions)]
    [InlineData(FileSystemRights.TakeOwnership)]
    public void MutatingRights_AreForbidden(FileSystemRights rights)
    {
        Assert.True(ProtectionVerifier.ContainsForbiddenNormalUserRights(rights));
    }
}
