using System.Security.Cryptography;

namespace FlashLock.Core;

public sealed record PinHash(string SaltBase64, string HashBase64, int Iterations)
{
    public const int DefaultIterations = 600_000;
}

public static class PinHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static PinHash Create(string pin, int iterations = PinHash.DefaultIterations)
    {
        ValidatePin(pin);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            pin,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return new(
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash),
            iterations);
    }

    public static bool Verify(string pin, PinHash stored)
    {
        ValidatePin(pin);

        var salt = Convert.FromBase64String(stored.SaltBase64);
        var expected = Convert.FromBase64String(stored.HashBase64);
        var actual = Rfc2898DeriveBytes.Pbkdf2(
            pin,
            salt,
            stored.Iterations,
            HashAlgorithmName.SHA256,
            expected.Length);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static void ValidatePin(string pin)
    {
        if (string.IsNullOrWhiteSpace(pin) || pin.Length < 6)
        {
            throw new ArgumentException("PIN/passphrase must contain at least 6 characters.", nameof(pin));
        }
    }
}
