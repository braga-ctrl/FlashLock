namespace FlashLock.Core;

public enum HelperAction
{
    Protect,
    Unlock,
    Recover
}

public sealed record HelperRequest(
    HelperAction Action,
    string DriveRoot,
    string Pin,
    string ExpectedVolumeSerialNumber);

public sealed record HelperResponse(
    bool Success,
    string Message,
    ProtectionState? State,
    int ObjectsProcessed = 0);
