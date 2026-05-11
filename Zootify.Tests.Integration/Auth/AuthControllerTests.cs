namespace Zootify.Tests.Integration.Auth;

public class AuthControllerTests : IntegrationTestBase
{
    public AuthControllerTests(IntegrationTestFixture fixture) : base(fixture) { }

    #region Register

    [Fact]
    public async Task Register_ValidRequest_Returns201AndCreatesUserWithPendingStatus()
    {
        Fixture.BrevoMock
            .Setup(b => b.SendRegistrationEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var response = await Client.PostAsJsonAsync(Routings.AuthRegister, new RegisterUserRequestDto
        {
            FirstName = "Max",
            LastName = "Mustermann",
            Email = "max@example.com",
            Password = "TestPass123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var user = await GetUserFromDbAsync("max@example.com");
        user.Should().NotBeNull();
        user!.Status.Should().Be(UserStatus.Pending);
        user.FirstName.Should().Be("Max");
        user.LastName.Should().Be("Mustermann");
        user.Email.Should().Be("max@example.com");
        user.RegistrationToken.Should().NotBeNull();
    }

    [Fact]
    public async Task Register_ValidRequest_SendsRegistrationEmail()
    {
        Fixture.BrevoMock
            .Setup(b => b.SendRegistrationEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        await Client.PostAsJsonAsync(Routings.AuthRegister, new RegisterUserRequestDto
        {
            FirstName = "Anna",
            LastName = "Test",
            Email = "anna@example.com",
            Password = "TestPass123!"
        });

        Fixture.BrevoMock.Verify(
            b => b.SendRegistrationEmail(
                It.IsAny<string>(), "anna@example.com", "Anna", "Test"),
            Times.Once);
    }

    [Fact]
    public async Task Register_EmailIsStoredLowercase()
    {
        Fixture.BrevoMock
            .Setup(b => b.SendRegistrationEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        await Client.PostAsJsonAsync(Routings.AuthRegister, new RegisterUserRequestDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "UPPERCASE@EXAMPLE.COM",
            Password = "TestPass123!"
        });

        var user = await GetUserFromDbAsync("uppercase@example.com");
        user.Should().NotBeNull();
        user!.Email.Should().Be("uppercase@example.com");
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400WithUserWithThisEmailAlreadyExists()
    {
        await _userRepo.CreateUserAsync("duplicate@test.com", status: UserStatus.Pending);

        var response = await Client.PostAsJsonAsync(Routings.AuthRegister, new RegisterUserRequestDto
        {
            FirstName = "Other",
            LastName = "User",
            Email = "duplicate@test.com",
            Password = "TestPass123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.UserWithThisEmailAlreadyExists);
    }

    [Fact]
    public async Task Register_InvalidEmail_Returns400WithEmailError()
    {
        var response = await Client.PostAsJsonAsync(Routings.AuthRegister, new RegisterUserRequestDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "notanemail",
            Password = "TestPass123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.EmailError);
    }

    [Fact]
    public async Task Register_PasswordTooShort_Returns400()
    {
        var response = await Client.PostAsJsonAsync(Routings.AuthRegister, new RegisterUserRequestDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "short@test.com",
            Password = "Ts1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.PasswordTooShort);
    }

    [Fact]
    public async Task Register_PasswordMissingUppercase_Returns400()
    {
        var response = await Client.PostAsJsonAsync(Routings.AuthRegister, new RegisterUserRequestDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "noup@test.com",
            Password = "testpass123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.PasswordMissingUppercase);
    }

    [Fact]
    public async Task Register_PasswordMissingLowercase_Returns400()
    {
        var response = await Client.PostAsJsonAsync(Routings.AuthRegister, new RegisterUserRequestDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "nolower@test.com",
            Password = "TESTPASS123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.PasswordMissingLowercase);
    }

    [Fact]
    public async Task Register_PasswordMissingDigit_Returns400()
    {
        var response = await Client.PostAsJsonAsync(Routings.AuthRegister, new RegisterUserRequestDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "nodigit@test.com",
            Password = "TestPassword!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.PasswordMissingDigit);
    }

    [Fact]
    public async Task Register_PasswordMissingSpecialCharacter_Returns400()
    {
        var response = await Client.PostAsJsonAsync(Routings.AuthRegister, new RegisterUserRequestDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "nospecial@test.com",
            Password = "TestPass123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.PasswordMissingSpecialCharacter);
    }

    #endregion

    #region ConfirmRegistration

    [Fact]
    public async Task ConfirmRegistration_ValidToken_Returns200AndActivatesUser()
    {
        var user = await _userRepo.CreateUserAsync("confirm@test.com", status: UserStatus.Pending);
        var token = user.RegistrationToken!.Token;

        var response = await Client.GetAsync($"{Routings.AuthConfirmRegistration}?token={token}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await GetUserFromDbAsync("confirm@test.com");
        updated!.Status.Should().Be(UserStatus.Active);
        updated.RegistrationToken.Should().BeNull();
    }

    [Fact]
    public async Task ConfirmRegistration_InvalidToken_Returns400WithNoUserWithThisId()
    {
        var response = await Client.GetAsync($"{Routings.AuthConfirmRegistration}?token=invalidtoken");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoUserWithThisId);
    }

    [Fact]
    public async Task ConfirmRegistration_AlreadyUsedToken_Returns400()
    {
        var user = await _userRepo.CreateUserAsync("used-token@test.com", status: UserStatus.Pending);
        var token = user.RegistrationToken!.Token;

        await Client.GetAsync($"{Routings.AuthConfirmRegistration}?token={token}");

        var response = await Client.GetAsync($"{Routings.AuthConfirmRegistration}?token={token}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoUserWithThisId);
    }

    [Fact]
    public async Task ConfirmRegistration_BlockedUser_BlockedViaLoginAttempts()
    {
        await _userRepo.CreateUserAsync("blocked@test.com");

        for (int i = 0; i < 5; i++)
        {
            await Client.PostAsJsonAsync(Routings.AuthLogin, new LoginUserRequestDto
            {
                Email = "blocked@test.com",
                Password = "WrongPassword!"
            });
        }

        var user = await GetUserFromDbAsync("blocked@test.com");
        user!.Status.Should().Be(UserStatus.Blocked);
    }

    #endregion

    #region SendNewRegistrationEmail

    [Fact]
    public async Task SendNewRegistrationEmail_PendingUser_Returns204AndEmailSent()
    {
        await _userRepo.CreateUserAsync("pending-new@test.com", status: UserStatus.Pending);

        Fixture.BrevoMock
            .Setup(b => b.SendRegistrationEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var response = await Client.PostAsJsonAsync(Routings.AuthNewRegistrationEmail,
            new SendNewRegistrationEmailRequestDto { Email = "pending-new@test.com" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        Fixture.BrevoMock.Verify(
            b => b.SendRegistrationEmail(
                It.IsAny<string>(), "pending-new@test.com", It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task SendNewRegistrationEmail_PendingUser_ReplacesRegistrationToken()
    {
        var user = await _userRepo.CreateUserAsync("token-replace@test.com", status: UserStatus.Pending);
        var oldToken = user.RegistrationToken!.Token;

        Fixture.BrevoMock
            .Setup(b => b.SendRegistrationEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        await Client.PostAsJsonAsync(Routings.AuthNewRegistrationEmail,
            new SendNewRegistrationEmailRequestDto { Email = "token-replace@test.com" });

        var updated = await GetUserFromDbAsync("token-replace@test.com");
        updated!.RegistrationToken.Should().NotBeNull();
        updated.RegistrationToken!.Token.Should().NotBe(oldToken);
    }

    [Fact]
    public async Task SendNewRegistrationEmail_UnknownEmail_Returns400WithNoUserWithThisEmail()
    {
        var response = await Client.PostAsJsonAsync(Routings.AuthNewRegistrationEmail,
            new SendNewRegistrationEmailRequestDto { Email = "unknown@test.com" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoUserWithThisEmail);
    }

    [Fact]
    public async Task SendNewRegistrationEmail_ActiveUser_Returns500WithDatabaseError()
    {
        await _userRepo.CreateUserAsync("active-reg-email@test.com");

        var response = await Client.PostAsJsonAsync(Routings.AuthNewRegistrationEmail,
            new SendNewRegistrationEmailRequestDto { Email = "active-reg-email@test.com" });

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.DatabaseError);
    }

    #endregion

    #region Login

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithJwtToken()
    {
        await _userRepo.CreateUserAsync("login-ok@test.com");

        var response = await Client.PostAsJsonAsync(Routings.AuthLogin, new LoginUserRequestDto
        {
            Email = "login-ok@test.com",
            Password = "TestPass123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<LoginUserResponseDto>();
        dto!.Token.Should().NotBeNullOrEmpty();
        dto.ExpiresInMinutes.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Login_EmailIsCaseInsensitive()
    {
        await _userRepo.CreateUserAsync("case-login@test.com");

        var response = await Client.PostAsJsonAsync(Routings.AuthLogin, new LoginUserRequestDto
        {
            Email = "CASE-LOGIN@TEST.COM",
            Password = "TestPass123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns400WithNoUserWithThisEmail()
    {
        var response = await Client.PostAsJsonAsync(Routings.AuthLogin, new LoginUserRequestDto
        {
            Email = "nobody@test.com",
            Password = "TestPass123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoUserWithThisEmail);
    }

    [Fact]
    public async Task Login_PendingUser_Returns400WithUserPending()
    {
        await _userRepo.CreateUserAsync("pending-login@test.com", status: UserStatus.Pending);

        var response = await Client.PostAsJsonAsync(Routings.AuthLogin, new LoginUserRequestDto
        {
            Email = "pending-login@test.com",
            Password = "TestPass123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.UserPending);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns400WithIncorrectPassword()
    {
        await _userRepo.CreateUserAsync("wrong-pw@test.com");

        var response = await Client.PostAsJsonAsync(Routings.AuthLogin, new LoginUserRequestDto
        {
            Email = "wrong-pw@test.com",
            Password = "WrongPassword!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.IncorrectPassword);
    }

    [Fact]
    public async Task Login_WrongPassword_IncrementsLoginAttempts()
    {
        await _userRepo.CreateUserAsync("attempts@test.com");

        await Client.PostAsJsonAsync(Routings.AuthLogin, new LoginUserRequestDto
        {
            Email = "attempts@test.com",
            Password = "Wrong!"
        });

        var user = await GetUserFromDbAsync("attempts@test.com");
        user!.LoginAttempts.Should().Be(1);
    }

    [Fact]
    public async Task Login_FiveWrongPasswords_BlocksUserAndReturnsUserIsBlocked()
    {
        var email = "block-me@test.com";
        await _userRepo.CreateUserAsync(email);

        for (int i = 0; i < 5; i++)
        {
            await Client.PostAsJsonAsync(Routings.AuthLogin, new LoginUserRequestDto
            {
                Email = email,
                Password = "WrongPassword!"
            });
        }

        var user = await GetUserFromDbAsync(email);
        user!.Status.Should().Be(UserStatus.Blocked);

        var response = await Client.PostAsJsonAsync(Routings.AuthLogin, new LoginUserRequestDto
        {
            Email = email,
            Password = "TestPass123!"
        });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.UserIsBlocked);
    }

    [Fact]
    public async Task Login_Success_ResetsLoginAttempts()
    {
        var email = "reset-attempts@test.com";
        await _userRepo.CreateUserAsync(email);

        await Client.PostAsJsonAsync(Routings.AuthLogin, new LoginUserRequestDto
        {
            Email = email,
            Password = "Wrong!"
        });

        await Client.PostAsJsonAsync(Routings.AuthLogin, new LoginUserRequestDto
        {
            Email = email,
            Password = "TestPass123!"
        });

        var user = await GetUserFromDbAsync(email);
        user!.LoginAttempts.Should().Be(0);
    }

    #endregion

    #region SendNewResetPassword

    [Fact]
    public async Task SendNewResetPasswordEmail_ActiveUser_Returns204AndEmailSentAndTokenCreated()
    {
        await _userRepo.CreateUserAsync("reset-req@test.com");

        Fixture.BrevoMock
            .Setup(b => b.SendResetPasswordEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var response = await Client.PostAsJsonAsync(Routings.AuthNewResetPassword,
            new SendNewResetPasswordEmailRequestDto { Email = "reset-req@test.com" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        Fixture.BrevoMock.Verify(
            b => b.SendResetPasswordEmail(
                It.IsAny<string>(), "reset-req@test.com", It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);

        var user = await GetUserFromDbAsync("reset-req@test.com");
        user!.ResetPasswordToken.Should().NotBeNull();
    }

    [Fact]
    public async Task SendNewResetPasswordEmail_UnknownEmail_Returns400WithNoUserWithThisEmail()
    {
        var response = await Client.PostAsJsonAsync(Routings.AuthNewResetPassword,
            new SendNewResetPasswordEmailRequestDto { Email = "nobody@test.com" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoUserWithThisEmail);
    }

    [Fact]
    public async Task SendNewResetPasswordEmail_PendingUser_Returns400WithUserPending()
    {
        await _userRepo.CreateUserAsync("pending-reset@test.com", status: UserStatus.Pending);

        var response = await Client.PostAsJsonAsync(Routings.AuthNewResetPassword,
            new SendNewResetPasswordEmailRequestDto { Email = "pending-reset@test.com" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.UserPending);
    }

    #endregion

    #region ResetPassword

    [Fact]
    public async Task ResetPassword_ValidToken_Returns200AndCanLoginWithNewPassword()
    {
        var user = await _userRepo.CreateUserAsync("reset-ok@test.com", withResetToken: true);
        var resetToken = user.ResetPasswordToken!.Token;

        Fixture.BrevoMock
            .Setup(b => b.SendConfirmationResetPasswordEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var response = await Client.PostAsJsonAsync(
            $"{Routings.AuthResetPassword}?resetPasswordToken={resetToken}",
            new ResetPasswordRequestDto
            {
                Password = "NewPass456!",
                RepeatedPassword = "NewPass456!"
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var oldLoginResponse = await Client.PostAsJsonAsync(Routings.AuthLogin, new LoginUserRequestDto
        {
            Email = "reset-ok@test.com",
            Password = "TestPass123!"
        });
        oldLoginResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var newLoginResponse = await Client.PostAsJsonAsync(Routings.AuthLogin, new LoginUserRequestDto
        {
            Email = "reset-ok@test.com",
            Password = "NewPass456!"
        });
        newLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetPassword_ValidToken_RemovesResetTokenFromDb()
    {
        var user = await _userRepo.CreateUserAsync("reset-token-remove@test.com", withResetToken: true);
        var resetToken = user.ResetPasswordToken!.Token;

        Fixture.BrevoMock
            .Setup(b => b.SendConfirmationResetPasswordEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        await Client.PostAsJsonAsync(
            $"{Routings.AuthResetPassword}?resetPasswordToken={resetToken}",
            new ResetPasswordRequestDto { Password = "NewPass456!", RepeatedPassword = "NewPass456!" });

        var updated = await GetUserFromDbAsync("reset-token-remove@test.com");
        updated!.ResetPasswordToken.Should().BeNull();
    }

    [Fact]
    public async Task ResetPassword_ValidToken_SendsConfirmationEmail()
    {
        var user = await _userRepo.CreateUserAsync("reset-confirm-email@test.com", withResetToken: true);
        var resetToken = user.ResetPasswordToken!.Token;

        Fixture.BrevoMock
            .Setup(b => b.SendConfirmationResetPasswordEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        await Client.PostAsJsonAsync(
            $"{Routings.AuthResetPassword}?resetPasswordToken={resetToken}",
            new ResetPasswordRequestDto { Password = "NewPass456!", RepeatedPassword = "NewPass456!" });

        Fixture.BrevoMock.Verify(
            b => b.SendConfirmationResetPasswordEmail(
                It.IsAny<string>(), It.IsAny<string>(), "reset-confirm-email@test.com"),
            Times.Once);
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_Returns400WithInvalidResetPasswordToken()
    {
        var response = await Client.PostAsJsonAsync(
            $"{Routings.AuthResetPassword}?resetPasswordToken=invalidtoken",
            new ResetPasswordRequestDto { Password = "NewPass456!", RepeatedPassword = "NewPass456!" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.InvalidResetPasswordToken);
    }

    [Fact]
    public async Task ResetPassword_PasswordsDoNotMatch_Returns400WithPasswordNotTheSame()
    {
        var user = await _userRepo.CreateUserAsync("reset-mismatch@test.com", withResetToken: true);
        var resetToken = user.ResetPasswordToken!.Token;

        var response = await Client.PostAsJsonAsync(
            $"{Routings.AuthResetPassword}?resetPasswordToken={resetToken}",
            new ResetPasswordRequestDto
            {
                Password = "NewPass456!",
                RepeatedPassword = "DifferentPass789!"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.PasswordNotTheSame);
    }

    [Fact]
    public async Task ResetPassword_WeakNewPassword_Returns400WithPasswordTooShort()
    {
        var user = await _userRepo.CreateUserAsync("reset-weak@test.com", withResetToken: true);
        var resetToken = user.ResetPasswordToken!.Token;

        var response = await Client.PostAsJsonAsync(
            $"{Routings.AuthResetPassword}?resetPasswordToken={resetToken}",
            new ResetPasswordRequestDto { Password = "Ts1!", RepeatedPassword = "Ts1!" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.PasswordTooShort);
    }

    [Fact]
    public async Task ResetPassword_ResetsLoginAttemptsAndKeepsActiveStatus()
    {
        var email = "reset-attempts2@test.com";
        var user = await _userRepo.CreateUserAsync(email, withResetToken: true);
        var resetToken = user.ResetPasswordToken!.Token;

        Fixture.BrevoMock
            .Setup(b => b.SendConfirmationResetPasswordEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        await Client.PostAsJsonAsync(Routings.AuthLogin,
            new LoginUserRequestDto { Email = email, Password = "Wrong!" });

        await Client.PostAsJsonAsync(
            $"{Routings.AuthResetPassword}?resetPasswordToken={resetToken}",
            new ResetPasswordRequestDto { Password = "NewPass456!", RepeatedPassword = "NewPass456!" });

        var updated = await GetUserFromDbAsync(email);
        updated!.LoginAttempts.Should().Be(0);
        updated.Status.Should().Be(UserStatus.Active);
    }

    #endregion
}
