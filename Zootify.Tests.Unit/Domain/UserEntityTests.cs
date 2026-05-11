namespace Zootify.Tests.Unit.Domain;

public class UserEntityTests
{
    #region FormatEmail

    [Fact]
    public void FormatEmail_ConvertsEmailToLowercase()
    {
        var user = new UserEntityBuilder().WithEmail("TEST@EXAMPLE.COM").Build();

        user.FormatEmail();

        user.Email.Should().Be("test@example.com");
    }

    [Fact]
    public void FormatEmail_AlreadyLowercase_NoChange()
    {
        var user = new UserEntityBuilder().WithEmail("test@example.com").Build();

        user.FormatEmail();

        user.Email.Should().Be("test@example.com");
    }

    #endregion

    #region ActivateUser

    [Fact]
    public void ActivateUser_SetsStatusToActive()
    {
        var user = UserTestData.CreateUser();

        user.ActivateUser();

        user.Status.Should().Be(UserStatus.Active);
    }

    #endregion

    #region SetPassword

    [Fact]
    public void SetPassword_StoresHashedPassword()
    {
        var user = UserTestData.CreateUser();

        user.SetPassword("MyPassword123!");

        user.PasswordHash.Should().NotBeNullOrEmpty();
        user.PasswordHash.Should().NotBe("MyPassword123!");
    }

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

    [Fact]
    public void CheckPassword_CorrectPassword_ReturnsTrue()
    {
        var user = UserTestData.CreateUser();
        user.SetPassword("CorrectPass123!");

        user.CheckPassword("CorrectPass123!").Should().BeTrue();
    }

    [Fact]
    public void CheckPassword_WrongPassword_ReturnsFalse()
    {
        var user = UserTestData.CreateUser();
        user.SetPassword("CorrectPass123!");

        user.CheckPassword("WrongPass456!").Should().BeFalse();
    }

    #endregion

    #region CreateRegistrationToken

    [Fact]
    public void CreateRegistrationToken_SetsRegistrationTokenOnEntity()
    {
        var user = UserTestData.CreateUser();

        var token = user.CreateRegistrationToken(expirationInHours: 24);

        token.Should().NotBeNull();
        user.RegistrationToken.Should().BeSameAs(token);
    }

    [Fact]
    public void CreateRegistrationToken_TokenIsNotEmpty()
    {
        var user = UserTestData.CreateUser();

        var token = user.CreateRegistrationToken(expirationInHours: 24);

        token.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreateRegistrationToken_ExpiresInFuture()
    {
        var user = UserTestData.CreateUser();

        var token = user.CreateRegistrationToken(expirationInHours: 24);

        token.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

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

    [Fact]
    public void CreateResetPasswordTokenEntityToken_SetsResetPasswordTokenOnEntity()
    {
        var user = UserTestData.CreateUser();

        var token = user.CreateResetPasswordTokenEntityToken(expirationInHours: 24);

        token.Should().NotBeNull();
        user.ResetPasswordToken.Should().BeSameAs(token);
    }

    [Fact]
    public void CreateResetPasswordTokenEntityToken_TokenIsNotEmpty()
    {
        var user = UserTestData.CreateUser();

        var token = user.CreateResetPasswordTokenEntityToken(expirationInHours: 24);

        token.Token.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region RemoveResetPasswordToken

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

    [Fact]
    public void IncrementLoginAttempts_IncrementsCounter()
    {
        var user = UserTestData.CreateUser();
        user.ActivateUser();

        user.IncrementLoginAttempts();

        user.LoginAttempts.Should().Be(1);
    }

    [Fact]
    public void IncrementLoginAttempts_CalledMultipleTimes_CounterAccumulates()
    {
        var user = UserTestData.CreateUser();
        user.ActivateUser();

        user.IncrementLoginAttempts();
        user.IncrementLoginAttempts();
        user.IncrementLoginAttempts();

        user.LoginAttempts.Should().Be(3);
    }

    [Fact]
    public void IncrementLoginAttempts_AtFifthAttempt_BlocksUser()
    {
        var user = UserTestData.CreateUser();
        user.ActivateUser();

        for (int i = 0; i < 5; i++)
            user.IncrementLoginAttempts();

        user.Status.Should().Be(UserStatus.Blocked);
    }

    [Fact]
    public void IncrementLoginAttempts_BelowFive_DoesNotBlockUser()
    {
        var user = UserTestData.CreateUser();
        user.ActivateUser();

        for (int i = 0; i < 4; i++)
            user.IncrementLoginAttempts();

        user.Status.Should().Be(UserStatus.Active);
    }

    #endregion

    #region ResetLoginAttempts

    [Fact]
    public void ResetLoginAttempts_SetsCounterToZero()
    {
        var user = UserTestData.CreateUser();
        user.ActivateUser();
        user.IncrementLoginAttempts();
        user.IncrementLoginAttempts();

        user.ResetLoginAttempts();

        user.LoginAttempts.Should().Be(0);
    }

    [Fact]
    public void ResetLoginAttempts_SetsStatusToActive()
    {
        var user = UserTestData.CreateUser();
        for (int i = 0; i < 5; i++)
            user.IncrementLoginAttempts();

        user.ResetLoginAttempts();

        user.Status.Should().Be(UserStatus.Active);
    }

    #endregion

    #region SetUpdatedBy

    [Fact]
    public void SetUpdatedBy_NullGuid_SetsUpdatedByUserGuidToNull()
    {
        var user = UserTestData.CreateUser();

        user.SetUpdatedBy(null);

        user.UpdatedByUserGuid.Should().BeNull();
        user.UpdatedDate.Should().NotBeNull();
    }

    [Fact]
    public void SetUpdatedBy_WithGuid_SetsUpdatedByUserGuid()
    {
        var user = UserTestData.CreateUser();
        var adminGuid = Guid.NewGuid();

        user.SetUpdatedBy(adminGuid);

        user.UpdatedByUserGuid.Should().Be(adminGuid);
    }

    #endregion
}
