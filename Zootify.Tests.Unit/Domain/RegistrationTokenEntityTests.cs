namespace Zootify.Tests.Unit.Domain;

/// <summary>
/// Unit tests for <see cref="RegistrationTokenEntity"/> factory method and expiry logic.
/// </summary>
public class RegistrationTokenEntityTests
{
    #region Create

    /// <summary>
    /// Verifies that <see cref="RegistrationTokenEntity.Create"/> returns a token with a non-empty token string.
    /// </summary>
    [Fact]
    public void Create_ReturnsTokenWithNonEmptyTokenString()
    {
        var token = RegistrationTokenEntity.Create(Guid.NewGuid(), TimeSpan.FromHours(24));

        token.Token.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies that the token string is a 128-character uppercase hex string
    /// (64 random bytes encoded as hex).
    /// </summary>
    [Fact]
    public void Create_TokenIsHexString()
    {
        var token = RegistrationTokenEntity.Create(Guid.NewGuid(), TimeSpan.FromHours(24));

        // 64 random bytes → 128 hex chars
        token.Token.Should().MatchRegex("^[0-9A-F]{128}$");
    }

    /// <summary>
    /// Verifies that the token's <c>UserGuid</c> matches the GUID passed to <see cref="RegistrationTokenEntity.Create"/>.
    /// </summary>
    [Fact]
    public void Create_SetsCorrectUserGuid()
    {
        var userGuid = Guid.NewGuid();

        var token = RegistrationTokenEntity.Create(userGuid, TimeSpan.FromHours(24));

        token.UserGuid.Should().Be(userGuid);
    }

    /// <summary>
    /// Verifies that the token's expiry is set in the future.
    /// </summary>
    [Fact]
    public void Create_ExpiresAtIsInFuture()
    {
        var token = RegistrationTokenEntity.Create(Guid.NewGuid(), TimeSpan.FromHours(24));

        token.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    /// <summary>
    /// Verifies that the token's expiry approximately matches the requested validity duration.
    /// </summary>
    [Fact]
    public void Create_ExpiresAtApproximatelyMatchesValidity()
    {
        var validity = TimeSpan.FromHours(24);

        var token = RegistrationTokenEntity.Create(Guid.NewGuid(), validity);

        token.ExpiresAt.Should()
            .BeCloseTo(DateTime.UtcNow.Add(validity), precision: TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Verifies that two calls with the same user GUID produce different token strings.
    /// </summary>
    [Fact]
    public void Create_TwoCallsProduceDifferentTokenStrings()
    {
        var userGuid = Guid.NewGuid();

        var token1 = RegistrationTokenEntity.Create(userGuid, TimeSpan.FromHours(24));
        var token2 = RegistrationTokenEntity.Create(userGuid, TimeSpan.FromHours(24));

        token1.Token.Should().NotBe(token2.Token);
    }

    /// <summary>
    /// Verifies that each created token is assigned a non-empty GUID as its own identifier.
    /// </summary>
    [Fact]
    public void Create_AssignsNewGuid()
    {
        var token = RegistrationTokenEntity.Create(Guid.NewGuid(), TimeSpan.FromHours(24));

        token.Guid.Should().NotBe(Guid.Empty);
    }

    #endregion

    #region IsExpired

    /// <summary>
    /// Verifies that <see cref="RegistrationTokenEntity.IsExpired"/> returns <c>false</c> for a token that expires in the future.
    /// </summary>
    [Fact]
    public void IsExpired_FutureExpiry_ReturnsFalse()
    {
        var token = RegistrationTokenEntity.Create(Guid.NewGuid(), TimeSpan.FromHours(24));

        token.IsExpired().Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="RegistrationTokenEntity.IsExpired"/> returns <c>true</c> for a token created with a past expiry.
    /// </summary>
    [Fact]
    public void IsExpired_PastExpiry_ReturnsTrue()
    {
        var token = RegistrationTokenEntity.Create(Guid.NewGuid(), TimeSpan.FromHours(-1));

        token.IsExpired().Should().BeTrue();
    }

    /// <summary>
    /// Verifies that a token created with a negative validity is immediately considered expired.
    /// </summary>
    [Fact]
    public void IsExpired_NegativeValidity_ReturnsTrue()
    {
        var token = RegistrationTokenEntity.Create(Guid.NewGuid(), TimeSpan.FromMilliseconds(-1));

        token.IsExpired().Should().BeTrue();
    }

    #endregion
}
