using System.Globalization;

namespace Zootify.Tests.Unit.Domain;

/// <summary>
/// Unit tests for the domain methods on <see cref="UserEntity"/>.
/// </summary>
public class UserEntityTests
{
    #region FormatEmail

    /// <summary>
    /// Verifies that <see cref="UserEntity.FormatEmail"/> converts an uppercase email to lowercase.
    /// </summary>
    [Fact]
    public void FormatEmail_ConvertsEmailToLowercase()
    {
        var user = new UserEntityBuilder().WithEmail("TEST@EXAMPLE.COM").Build();

        user.FormatEmail();

        user.Email.Should().Be("test@example.com");
    }

    /// <summary>
    /// Verifies that <see cref="UserEntity.FormatEmail"/> leaves an already lowercase email unchanged.
    /// </summary>
    [Fact]
    public void FormatEmail_AlreadyLowercase_NoChange()
    {
        var user = new UserEntityBuilder().WithEmail("test@example.com").Build();

        user.FormatEmail();

        user.Email.Should().Be("test@example.com");
    }

    #endregion

    #region ActivateUser

    /// <summary>
    /// Verifies that <see cref="UserEntity.ActivateUser"/> sets the status to Active.
    /// </summary>
    [Fact]
    public void ActivateUser_SetsStatusToActive()
    {
        var user = UserTestData.CreateUser();

        user.ActivateUser();

        user.Status.Should().Be(UserStatus.Active);
    }

    #endregion

    #region SetPassword

    /// <summary>
    /// Verifies that <see cref="UserEntity.SetPassword"/> stores a hashed value, not the plain text.
    /// </summary>
    [Fact]
    public void SetPassword_StoresHashedPassword()
    {
        var user = UserTestData.CreateUser();

        user.SetPassword("MyPassword123!");

        user.PasswordHash.Should().NotBeNullOrEmpty();
        user.PasswordHash.Should().NotBe("MyPassword123!");
    }

    /// <summary>
    /// Verifies that two different passwords produce different hashes (BCrypt uses a random salt).
    /// </summary>
    [Fact]
    public void SetPassword_DifferentPasswordsProduceDifferentHashes()
    {
        var user1 = UserTestData.CreateUser();
        var user2 = UserTestData.CreateUser();

        user1.SetPassword("Password123!");
        user2.SetPassword("OtherPass456!");

        user1.PasswordHash.Should().NotBe(user2.PasswordHash);
    }

    #endregion

    #region CheckPassword

    /// <summary>
    /// Verifies that <see cref="UserEntity.CheckPassword"/> returns <c>true</c> for the correct password.
    /// </summary>
    [Fact]
    public void CheckPassword_CorrectPassword_ReturnsTrue()
    {
        var user = UserTestData.CreateUser();
        user.SetPassword("CorrectPass123!");

        user.CheckPassword("CorrectPass123!").Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <see cref="UserEntity.CheckPassword"/> returns <c>false</c> for a wrong password.
    /// </summary>
    [Fact]
    public void CheckPassword_WrongPassword_ReturnsFalse()
    {
        var user = UserTestData.CreateUser();
        user.SetPassword("CorrectPass123!");

        user.CheckPassword("WrongPass456!").Should().BeFalse();
    }

    #endregion

    #region CreateRegistrationToken

    /// <summary>
    /// Verifies that <see cref="UserEntity.CreateRegistrationToken"/> attaches the returned token
    /// to the entity's navigation property.
    /// </summary>
    [Fact]
    public void CreateRegistrationToken_SetsRegistrationTokenOnEntity()
    {
        var user = UserTestData.CreateUser();

        var token = user.CreateRegistrationToken(expirationInHours: 24);

        token.Should().NotBeNull();
        user.RegistrationToken.Should().BeSameAs(token);
    }

    /// <summary>
    /// Verifies that the generated registration token string is not null or empty.
    /// </summary>
    [Fact]
    public void CreateRegistrationToken_TokenIsNotEmpty()
    {
        var user = UserTestData.CreateUser();

        var token = user.CreateRegistrationToken(expirationInHours: 24);

        token.Token.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies that the registration token expiry is set in the future.
    /// </summary>
    [Fact]
    public void CreateRegistrationToken_ExpiresInFuture()
    {
        var user = UserTestData.CreateUser();

        var token = user.CreateRegistrationToken(expirationInHours: 24);

        token.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    /// <summary>
    /// Verifies that two successive calls produce different token strings (cryptographic randomness).
    /// </summary>
    [Fact]
    public void CreateRegistrationToken_TwoCallsProduceDifferentTokens()
    {
        var user = UserTestData.CreateUser();

        var token1 = user.CreateRegistrationToken(expirationInHours: 24);
        var token2 = user.CreateRegistrationToken(expirationInHours: 24);

        token1.Token.Should().NotBe(token2.Token);
    }

    #endregion

    #region RemoveRegistrationToken

    /// <summary>
    /// Verifies that <see cref="UserEntity.RemoveRegistrationToken"/> sets the navigation property to <c>null</c>.
    /// </summary>
    [Fact]
    public void RemoveRegistrationToken_SetsRegistrationTokenToNull()
    {
        var user = UserTestData.CreateUser();
        user.CreateRegistrationToken(expirationInHours: 24);

        user.RemoveRegistrationToken();

        user.RegistrationToken.Should().BeNull();
    }

    #endregion

    #region CreateResetPasswordToken

    /// <summary>
    /// Verifies that <see cref="UserEntity.CreateResetPasswordTokenEntityToken"/> attaches the returned token
    /// to the entity's navigation property.
    /// </summary>
    [Fact]
    public void CreateResetPasswordTokenEntityToken_SetsResetPasswordTokenOnEntity()
    {
        var user = UserTestData.CreateUser();

        var token = user.CreateResetPasswordTokenEntityToken(expirationInHours: 24);

        token.Should().NotBeNull();
        user.ResetPasswordToken.Should().BeSameAs(token);
    }

    /// <summary>
    /// Verifies that the generated reset-password token string is not null or empty.
    /// </summary>
    [Fact]
    public void CreateResetPasswordTokenEntityToken_TokenIsNotEmpty()
    {
        var user = UserTestData.CreateUser();

        var token = user.CreateResetPasswordTokenEntityToken(expirationInHours: 24);

        token.Token.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region RemoveResetPasswordToken

    /// <summary>
    /// Verifies that <see cref="UserEntity.RemoveResetPasswordToken"/> sets the navigation property to <c>null</c>.
    /// </summary>
    [Fact]
    public void RemoveResetPasswordToken_SetsResetPasswordTokenToNull()
    {
        var user = UserTestData.CreateUser();
        user.CreateResetPasswordTokenEntityToken(expirationInHours: 24);

        user.RemoveResetPasswordToken();

        user.ResetPasswordToken.Should().BeNull();
    }

    #endregion

    #region IncrementLoginAttempts

    /// <summary>
    /// Verifies that a single <see cref="UserEntity.IncrementLoginAttempts"/> call increments the counter to 1.
    /// </summary>
    [Fact]
    public void IncrementLoginAttempts_IncrementsCounter()
    {
        var user = UserTestData.CreateUser();

        user.IncrementLoginAttempts(5);

        user.LoginAttempts.Should().Be(1);
    }

    /// <summary>
    /// Verifies that multiple calls to <see cref="UserEntity.IncrementLoginAttempts"/> accumulate correctly.
    /// </summary>
    [Fact]
    public void IncrementLoginAttempts_CalledMultipleTimes_CounterAccumulates()
    {
        var user = UserTestData.CreateUser();

        user.IncrementLoginAttempts(5);
        user.IncrementLoginAttempts(5);
        user.IncrementLoginAttempts(5);

        user.LoginAttempts.Should().Be(3);
    }

    /// <summary>
    /// Verifies that the user is set to Blocked after exactly 5 failed attempts.
    /// </summary>
    [Fact]
    public void IncrementLoginAttempts_AtFifthAttempt_BlocksUser()
    {
        var user = UserTestData.CreateUser();

        for (var i = 0; i < 5; i++)
        {
            user.IncrementLoginAttempts(5);
        }

        user.Status.Should().Be(UserStatus.Blocked);
    }

    /// <summary>
    /// Verifies that 4 failed attempts do not yet block the user.
    /// </summary>
    [Fact]
    public void IncrementLoginAttempts_BelowFive_DoesNotBlockUser()
    {
        var user = UserTestData.CreateUser();
        user.ActivateUser();

        for (var i = 0; i < 4; i++)
        {
            user.IncrementLoginAttempts(5);
        }

        user.Status.Should().Be(UserStatus.Active);
    }

    #endregion

    #region CreateUserSettings

    /// <summary>
    /// Tests Create User Settings – Sets Settings On Entity.
    /// </summary>
    [Fact]
    public void CreateUserSettings_SetsSettingsOnEntity()
    {
        var user = UserTestData.CreateUser();

        user.CreateUserSettings();

        user.Settings.Should().NotBeNull();
    }

    /// <summary>
    /// Tests Create User Settings – Sets User Guid.
    /// </summary>
    [Fact]
    public void CreateUserSettings_SetsUserGuid()
    {
        var user = UserTestData.CreateUser();

        user.CreateUserSettings();

        user.Settings.UserGuid.Should().Be(user.Guid);
    }

    /// <summary>
    /// Tests Create User Settings – Check Language.
    /// </summary>
    [Fact]
    public void CreateUserSettings_CheckLanguage()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("de");

        var user = UserTestData.CreateUser();

        user.CreateUserSettings();

        user.Settings.Language.Should().Be("de");
    }

    /// <summary>
    /// Tests Create User Settings – Default Dark Mode Is False.
    /// </summary>
    [Fact]
    public void CreateUserSettings_DefaultDarkModeIsFalse()
    {
        var user = UserTestData.CreateUser();

        user.CreateUserSettings();

        user.Settings.DarkMode.Should().BeFalse();
    }

    #endregion

    #region ResetLoginAttempts

    /// <summary>
    /// Verifies that <see cref="UserEntity.ResetLoginAttempts"/> sets the counter back to zero.
    /// </summary>
    [Fact]
    public void ResetLoginAttempts_SetsCounterToZero()
    {
        var user = UserTestData.CreateUser();
        user.IncrementLoginAttempts(5);
        user.IncrementLoginAttempts(5);

        user.ResetLoginAttempts();

        user.LoginAttempts.Should().Be(0);
    }

    /// <summary>
    /// Verifies that <see cref="UserEntity.ResetLoginAttempts"/> also restores the status to Active,
    /// even when the user was previously blocked.
    /// </summary>
    [Fact]
    public void ResetLoginAttempts_SetsStatusToActive()
    {
        var user = UserTestData.CreateUser();
        for (var i = 0; i < 5; i++)
        {
            user.IncrementLoginAttempts(5);
        }

        user.ActivateUser();

        user.Status.Should().Be(UserStatus.Active);
    }

    #endregion
}
