using FlashLock.Core;

namespace FlashLock.Core.Tests;

public sealed class PinHasherTests
{
    [Fact]
    public void CreateAndVerify_RoundTrips()
    {
        var stored = PinHasher.Create("correct horse battery staple", iterations: 10_000);

        Assert.True(PinHasher.Verify("correct horse battery staple", stored));
        Assert.False(PinHasher.Verify("wrong password", stored));
    }

    [Fact]
    public void Create_RejectsShortPin()
    {
        Assert.Throws<ArgumentException>(() => PinHasher.Create("12345"));
    }
}
