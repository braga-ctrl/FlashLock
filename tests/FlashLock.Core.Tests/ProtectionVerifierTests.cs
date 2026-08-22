using System.Security.AccessControl;
using FlashLock.Core;

namespace FlashLock.Core.Tests;

public sealed class ProtectionVerifierTests
{
    [Fact]
    public void ProtectedProfile_BlocksEveryoneWriteRights()
    {
        var rights = FileSystemRights.ReadAndExecute;
        var forbidden = FileSystemRights.Write
            | FileSystemRights.Modify
            | FileSystemRights.Delete
            | FileSystemRights.DeleteSubdirectoriesAndFiles
            | FileSystemRights.ChangePermissions
            | FileSystemRights.TakeOwnership;

        Assert.Equal(FileSystemRights.ReadAndExecute, rights & FileSystemRights.ReadAndExecute);
        Assert.Equal((FileSystemRights)0, rights & forbidden);
    }
}
