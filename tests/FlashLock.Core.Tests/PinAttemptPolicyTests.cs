using FlashLock.Core;

namespace FlashLock.Core.Tests;

public sealed class PinAttemptPolicyTests
{
    [Fact]
    public void FiveFailures_StartTemporaryLockout()
    {
        var now = DateTimeOffset.UtcNow;
        var config = MakeConfig();

        for (var i = 0; i < PinAttemptPolicy.MaxFailures; i++)
        {
            config = PinAttemptPolicy.RecordFailure(config, now);
        }

        Assert.NotNull(config.PinLockoutUntilUtc);
        Assert.Throws<InvalidOperationException>(() => PinAttemptPolicy.ThrowIfLockedOut(config, now.AddSeconds(1)));
    }

    [Fact]
    public void Success_ClearsFailureState()
    {
        var config = MakeConfig() with { FailedPinAttempts = 3, PinLockoutUntilUtc = DateTimeOffset.UtcNow.AddSeconds(-1) };
        var result = PinAttemptPolicy.RecordSuccess(config);
        Assert.Equal(0, result.FailedPinAttempts);
        Assert.Null(result.PinLockoutUntilUtc);
    }

    private static FlashLockConfig MakeConfig() => new(
        FlashLockConfig.CurrentSchemaVersion,
        DateTimeOffset.UtcNow,
        PinHasher.Create("123456", 10_000),
        ProtectionState.Unlocked,
        "AABBCCDD",
        FlashLockConfig.DefaultSnapshotFileName,
        null,
        null,
        0,
        null);
}
