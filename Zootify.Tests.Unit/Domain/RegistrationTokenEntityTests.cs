namespace Zootify.Tests.Unit.Domain;

public class RegistrationTokenEntityTests
{
    #region Create

    [Fact]
    public void Create_ReturnsTokenWithNonEmptyTokenString()
    {
        var token = RegistrationTokenEntity.Create(Guid.NewGuid(), TimeSpan.FromHours(24));

        token.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Create_TokenIsHexString()
    {
        var token = RegistrationTokenEntity.Create(Guid.NewGuid(), TimeSpan.FromHours(24));

        // 64 random bytes → 128 hex chars
        token.Token.Should().MatchRegex("^[0-9A-F]{128}$");
    }

    [Fact]
    public void Create_SetsCorrectUserGuid()
    {
        var userGuid = Guid.NewGuid();

        var token = RegistrationTokenEntity.Create(userGuid, TimeSpan.FromHours(24));

        token.UserGuid.Should().Be(userGuid);
    }

    [Fact]
    public void Create_ExpiresAtIsInFuture()
    {
        var token = RegistrationTokenEntity.Create(Guid.NewGuid(), TimeSpan.FromHours(24));

        token.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void Create_ExpiresAtApproximatelyMatchesValidity()
    {
        var validity = TimeSpan.FromHours(24);

        var token = RegistrationTokenEntity.Create(Guid.NewGuid(), validity);

        token.ExpiresAt.Should()
            .BeCloseTo(DateTime.UtcNow.Add(validity), precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_TwoCallsProduceDifferentTokenStrings()
    {
        var userGuid = Guid.NewGuid();

        var token1 = RegistrationTokenEntity.Create(userGuid, TimeSpan.FromHours(24));
        var token2 = RegistrationTokenEntity.Create(userGuid, TimeSpan.FromHours(24));

        token1.Token.Should().NotBe(token2.Token);
    }

    [Fact]
    public void Create_AssignsNewGuid()
    {
        var token = RegistrationTokenEntity.Create(Guid.NewGuid(), TimeSpan.FromHours(24));

        token.Guid.Should().NotBe(Guid.Empty);
    }

    #endregion

    #region IsExpired

    [Fact]
    public void IsExpired_FutureExpiry_ReturnsFalse()
    {
        var token = RegistrationTokenEntity.Create(Guid.NewGuid(), TimeSpan.FromHours(24));

        token.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void IsExpired_PastExpiry_ReturnsTrue()
    {
        var token = RegistrationTokenEntity.Create(Guid.NewGuid(), TimeSpan.FromHours(-1));

        token.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void IsExpired_NegativeValidity_ReturnsTrue()
    {
        var token = RegistrationTokenEntity.Create(Guid.NewGuid(), TimeSpan.FromMilliseconds(-1));

        token.IsExpired().Should().BeTrue();
    }

    #endregion
}
