namespace FlashLock.Core;

public sealed record FlashLockConfig(
    int SchemaVersion,
    DateTimeOffset CreatedAtUtc,
    PinHash OwnerPin,
    string ProtectionMode,
    string? OriginalRootSddl)
{
    public const int CurrentSchemaVersion = 1;
}
