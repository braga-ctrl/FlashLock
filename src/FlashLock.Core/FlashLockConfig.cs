namespace FlashLock.Core;

public sealed record FlashLockConfig(
    int SchemaVersion,
    DateTimeOffset CreatedAtUtc,
    PinHash OwnerPin,
    ProtectionState State,
    string VolumeSerialNumber,
    string SnapshotFileName,
    DateTimeOffset? ProtectedAtUtc,
    DateTimeOffset? LastUnlockedAtUtc,
    int FailedPinAttempts,
    DateTimeOffset? PinLockoutUntilUtc)
{
    public const int CurrentSchemaVersion = 2;
    public const string DefaultSnapshotFileName = "acl-snapshot.jsonl";
}
