namespace FlashLock.Core;

public static class PinAttemptPolicy
{
    public const int MaxFailures = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromSeconds(30);

    public static void ThrowIfLockedOut(FlashLockConfig config, DateTimeOffset now)
    {
        if (config.PinLockoutUntilUtc is { } until && until > now)
        {
            var seconds = Math.Max(1, (int)Math.Ceiling((until - now).TotalSeconds));
            throw new InvalidOperationException($"Too many incorrect PIN attempts. Try again in {seconds} seconds.");
        }
    }

    public static FlashLockConfig RecordFailure(FlashLockConfig config, DateTimeOffset now)
    {
        var failures = config.FailedPinAttempts + 1;
        var lockout = failures >= MaxFailures ? now.Add(LockoutDuration) : config.PinLockoutUntilUtc;
        return config with
        {
            FailedPinAttempts = failures >= MaxFailures ? 0 : failures,
            PinLockoutUntilUtc = lockout
        };
    }

    public static FlashLockConfig RecordSuccess(FlashLockConfig config) =>
        config with { FailedPinAttempts = 0, PinLockoutUntilUtc = null };
}
