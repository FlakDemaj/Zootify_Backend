using Zootify_Backend.DTOs.Auth.Request;
using Zootify_Backend.DTOs.Auth.Response;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Auth;

public class AuthControllerTests : IntegrationTestBase
{
    public AuthControllerTests(IntegrationTestFixture fixture) : base(fixture) { }

    #region Register

    /// <summary>
    /// Verifies that a valid registration request returns 201 Created
    /// and persists the user with Pending status and a registration token.
    /// </summary>
    [Fact]
    public async Task Register_ValidRequest_Returns201AndCreatesUserWithPendingStatus()
    {
        Fixture.BrevoMock
            .Setup(b => b.SendRegistrationEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var request = new RegisterUserRequestDto
        {
            FirstName = "Max",
            LastName = "Mustermann",
            Email = "max@example.com",
            Password = "TestPass123!"
        };

        var response = await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/Register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var user = await GetUserFromDbByEmailAsync("max@example.com");
        user.Should().NotBeNull();
        user!.Status.Should().Be(UserStatus.Pending);
        user.FirstName.Should().Be("Max");
        user.LastName.Should().Be("Mustermann");
        user.Email.Should().Be("max@example.com");
        user.RegistrationToken.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that a valid registration request triggers exactly one registration email via Brevo.
    /// </summary>
    [Fact]
    public async Task Register_ValidRequest_SendsRegistrationEmail()
    {
        Fixture.BrevoMock
            .Setup(b => b.SendRegistrationEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var request = new RegisterUserRequestDto
        {
            FirstName = "Anna",
            LastName = "Test",
            Email = "anna@example.com",
            Password = "TestPass123!"
        };

        await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/Register", request);

        Fixture.BrevoMock.Verify(
            b => b.SendRegistrationEmail(
                It.IsAny<string>(), "anna@example.com", "Anna", "Test"),
            Times.Once);
    }

    /// <summary>
    /// Verifies that the email address is normalized to lowercase before being persisted.
    /// </summary>
    [Fact]
    public async Task Register_EmailIsStoredLowercase()
    {
        Fixture.BrevoMock
            .Setup(b => b.SendRegistrationEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var request = new RegisterUserRequestDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "UPPERCASE@EXAMPLE.COM",
            Password = "TestPass123!"
        };

        await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/Register", request);

        var user = await GetUserFromDbByEmailAsync("uppercase@example.com");
        user.Should().NotBeNull();
        user!.Email.Should().Be("uppercase@example.com");
    }

    /// <summary>
    /// Verifies that registering with an already existing email returns 400
    /// with the <see cref="ErrorCode.UserWithThisEmailAlreadyExists"/> error code.
    /// </summary>
    [Fact]
    public async Task Register_DuplicateEmail_Returns400WithUserWithThisEmailAlreadyExists()
    {
        await UserRepo.CreateUserAsync("duplicate@test.com", status: UserStatus.Pending);

        var request = new RegisterUserRequestDto
        {
            FirstName = "Other",
            LastName = "User",
            Email = "duplicate@test.com",
            Password = "TestPass123!"
        };

        var response = await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/Register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.UserWithThisEmailAlreadyExists);
    }

    /// <summary>
    /// Verifies that an invalid email format returns 400 with the <see cref="ErrorCode.EmailError"/> error code.
    /// </summary>
    [Fact]
    public async Task Register_InvalidEmail_Returns400WithEmailError()
    {
        var request = new RegisterUserRequestDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "notanemail",
            Password = "TestPass123!"
        };

        var response = await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/Register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.EmailError);
    }

    /// <summary>
    /// Verifies that a password below the minimum length returns 400
    /// with the <see cref="ErrorCode.PasswordTooShort"/> error code.
    /// </summary>
    [Fact]
    public async Task Register_PasswordTooShort_Returns400()
    {
        var request = new RegisterUserRequestDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "short@test.com",
            Password = "Ts1!"
        };

        var response = await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/Register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.PasswordTooShort);
    }

    /// <summary>
    /// Verifies that a password without an uppercase letter returns 400
    /// with the <see cref="ErrorCode.PasswordMissingUppercase"/> error code.
    /// </summary>
    [Fact]
    public async Task Register_PasswordMissingUppercase_Returns400()
    {
        var request = new RegisterUserRequestDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "noup@test.com",
            Password = "testpass123!"
        };

        var response = await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/Register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.PasswordMissingUppercase);
    }

    /// <summary>
    /// Verifies that a password without a lowercase letter returns 400
    /// with the <see cref="ErrorCode.PasswordMissingLowercase"/> error code.
    /// </summary>
    [Fact]
    public async Task Register_PasswordMissingLowercase_Returns400()
    {
        var request = new RegisterUserRequestDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "nolower@test.com",
            Password = "TESTPASS123!"
        };

        var response = await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/Register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.PasswordMissingLowercase);
    }

    /// <summary>
    /// Verifies that a password without a digit returns 400
    /// with the <see cref="ErrorCode.PasswordMissingDigit"/> error code.
    /// </summary>
    [Fact]
    public async Task Register_PasswordMissingDigit_Returns400()
    {
        var request = new RegisterUserRequestDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "nodigit@test.com",
            Password = "TestPassword!"
        };

        var response = await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/Register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.PasswordMissingDigit);
    }

    /// <summary>
    /// Verifies that a password without a special character returns 400
    /// with the <see cref="ErrorCode.PasswordMissingSpecialCharacter"/> error code.
    /// </summary>
    [Fact]
    public async Task Register_PasswordMissingSpecialCharacter_Returns400()
    {
        var request = new RegisterUserRequestDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "nospecial@test.com",
            Password = "TestPass123"
        };

        var response = await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/Register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.PasswordMissingSpecialCharacter);
    }

    #endregion

    #region ConfirmRegistration

    /// <summary>
    /// Verifies that a valid registration token activates the user and removes the token from the database.
    /// </summary>
    [Fact]
    public async Task ConfirmRegistration_ValidToken_Returns200AndActivatesUser()
    {
        var user = await UserRepo.CreateUserAsync("confirm@test.com", status: UserStatus.Pending);
        var token = user.RegistrationToken!.Token;

        var response = await Client.GetAsync($"{Routings.SystemAuthRouting}/confirm-registration?token={token}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await GetUserFromDbByEmailAsync("confirm@test.com");
        updated!.Status.Should().Be(UserStatus.Active);
        updated.RegistrationToken.Should().BeNull();
    }

    /// <summary>
    /// Verifies that a non-existent token returns 400
    /// with the <see cref="ErrorCode.NoUserWithThisToken"/> error code.
    /// </summary>
    [Fact]
    public async Task ConfirmRegistration_InvalidToken_Returns400WithNoUserWithThisToken()
    {
        var response = await Client.GetAsync($"{Routings.SystemAuthRouting}/confirm-registration?token=invalidtoken");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoUserWithThisToken);
    }

    /// <summary>
    /// Verifies that reusing an already consumed registration token returns 400.
    /// The token is deleted after the first successful confirmation, so the second call finds no match.
    /// </summary>
    [Fact]
    public async Task ConfirmRegistration_AlreadyUsedToken_Returns400()
    {
        var user = await UserRepo.CreateUserAsync("used-token@test.com", status: UserStatus.Pending);
        var token = user.RegistrationToken!.Token;

        // first call consumes the token
        await Client.GetAsync($"{Routings.SystemAuthRouting}/confirm-registration?token={token}");

        var response = await Client.GetAsync($"{Routings.SystemAuthRouting}/confirm-registration?token={token}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoUserWithThisToken);
    }

    /// <summary>
    /// Verifies that a user is set to Blocked status after 5 consecutive failed login attempts.
    /// </summary>
    [Fact]
    public async Task ConfirmRegistration_BlockedUser_BlockedViaLoginAttempts()
    {
        await UserRepo.CreateUserAsync(
            "blocked@test.com",
            wrongLoginAttempts: 4);

        var request = new LoginUserRequestDto
        {
            Email = "blocked@test.com",
            Password = "WrongPassword!"
        };

        await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/login", request);
        
        var user = await GetUserFromDbByEmailAsync("blocked@test.com");
        user!.Status.Should().Be(UserStatus.Blocked);
    }

    #endregion

    #region SendNewRegistrationEmail

    /// <summary>
    /// Verifies that a pending user receives a new registration email and the response is 204 No Content.
    /// </summary>
    [Fact]
    public async Task SendNewRegistrationEmail_PendingUser_Returns204AndEmailSent()
    {
        await UserRepo.CreateUserAsync("pending-new@test.com", status: UserStatus.Pending);

        Fixture.BrevoMock
            .Setup(b => b.SendRegistrationEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var request = new SendNewRegistrationEmailRequestDto { Email = "pending-new@test.com" };

        var response = await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/new-registration-email", request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        Fixture.BrevoMock.Verify(
            b => b.SendRegistrationEmail(
                It.IsAny<string>(), "pending-new@test.com", It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that requesting a new registration email replaces the existing token with a fresh one.
    /// </summary>
    [Fact]
    public async Task SendNewRegistrationEmail_PendingUser_ReplacesRegistrationToken()
    {
        var user = await UserRepo.CreateUserAsync("token-replace@test.com", status: UserStatus.Pending);
        var oldToken = user.RegistrationToken!.Token;

        Fixture.BrevoMock
            .Setup(b => b.SendRegistrationEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var request = new SendNewRegistrationEmailRequestDto { Email = "token-replace@test.com" };

        await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/new-registration-email", request);

        var updated = await GetUserFromDbByEmailAsync("token-replace@test.com");
        updated!.RegistrationToken.Should().NotBeNull();
        updated.RegistrationToken!.Token.Should().NotBe(oldToken);
    }

    /// <summary>
    /// Verifies that requesting a new registration email for an unknown address returns 400
    /// with the <see cref="ErrorCode.NoUserWithThisEmail"/> error code.
    /// </summary>
    [Fact]
    public async Task SendNewRegistrationEmail_UnknownEmail_Returns400WithNoUserWithThisEmail()
    {
        var request = new SendNewRegistrationEmailRequestDto { Email = "unknown@test.com" };

        var response = await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/new-registration-email", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoUserWithThisEmail);
    }

    /// <summary>
    /// Verifies that requesting a new registration email for an already active user returns 500
    /// with the <see cref="ErrorCode.DatabaseError"/> error code.
    /// An active user has no pending registration token, causing the DB operation to fail.
    /// </summary>
    [Fact]
    public async Task SendNewRegistrationEmail_ActiveUser_Returns500WithDatabaseError()
    {
        await UserRepo.CreateUserAsync("active-reg-email@test.com");

        var request = new SendNewRegistrationEmailRequestDto { Email = "active-reg-email@test.com" };

        var response = await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/new-registration-email", request);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.DatabaseError);
    }

    #endregion

    #region Login

    /// <summary>
    /// Verifies that valid credentials return 200 with a non-empty JWT token and a positive expiry.
    /// </summary>
    [Fact]
    public async Task Login_ValidCredentials_Returns200WithJwtToken()
    {
        var user = await UserRepo.CreateUserAsync("login-ok@test.com");

        var request = new LoginUserRequestDto
        {
            Email = "login-ok@test.com",
            Password = "TestPass123!"
        };

        var response = await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<LoginUserResponseDto>();
        dto.Should().NotBeNull();
        dto!.Token.Token.Should().NotBeNullOrEmpty();
        dto.Token.ExpiresInMinutes.Should().BeGreaterThan(0);
        dto.Name.Should().Be(user.FirstName + " "  + user.LastName);
    }

    /// <summary>
    /// Verifies that the email lookup during login is case-insensitive.
    /// </summary>
    [Fact]
    public async Task Login_EmailIsCaseInsensitive()
    {
        await UserRepo.CreateUserAsync("case-login@test.com");

        var request = new LoginUserRequestDto
        {
            Email = "CASE-LOGIN@TEST.COM",
            Password = "TestPass123!"
        };

        var response = await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Verifies that logging in with an unknown email returns 400
    /// with the <see cref="ErrorCode.NoUserWithThisEmail"/> error code.
    /// </summary>
    [Fact]
    public async Task Login_UnknownEmail_Returns400WithNoUserWithThisEmail()
    {
        var request = new LoginUserRequestDto
        {
            Email = "nobody@test.com",
            Password = "TestPass123!"
        };

        var response = await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoUserWithThisEmail);
    }

    /// <summary>
    /// Verifies that a user with Pending status cannot log in and receives 400
    /// with the <see cref="ErrorCode.UserPending"/> error code.
    /// </summary>
    [Fact]
    public async Task Login_PendingUser_Returns400WithUserPending()
    {
        await UserRepo.CreateUserAsync("pending-login@test.com", status: UserStatus.Pending);

        var request = new LoginUserRequestDto
        {
            Email = "pending-login@test.com",
            Password = "TestPass123!"
        };

        var response = await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.UserPending);
    }

    /// <summary>
    /// Verifies that a wrong password returns 400 with the <see cref="ErrorCode.IncorrectPassword"/> error code.
    /// </summary>
    [Fact]
    public async Task Login_WrongPassword_Returns400WithIncorrectPassword()
    {
        await UserRepo.CreateUserAsync("wrong-pw@test.com");

        var request = new LoginUserRequestDto
        {
            Email = "wrong-pw@test.com",
            Password = "WrongPassword!"
        };

        var response = await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.IncorrectPassword);
    }

    /// <summary>
    /// Verifies that a failed login attempt increments the user's LoginAttempts counter by one.
    /// </summary>
    [Fact]
    public async Task Login_WrongPassword_IncrementsLoginAttempts()
    {
        await UserRepo.CreateUserAsync("attempts@test.com");

        var request = new LoginUserRequestDto
        {
            Email = "attempts@test.com",
            Password = "Wrong!"
        };

        await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/login", request);

        var user = await GetUserFromDbByEmailAsync("attempts@test.com");
        user!.LoginAttempts.Should().Be(1);
    }

    /// <summary>
    /// Verifies that after 5 consecutive failed login attempts the user is blocked,
    /// and any subsequent login — even with the correct password — returns 400
    /// with the <see cref="ErrorCode.UserIsBlocked"/> error code.
    /// </summary>
    [Fact]
    public async Task Login_FiveWrongPasswords_BlocksUserAndReturnsUserIsBlocked()
    {
        var email = "block-me@test.com";
        await UserRepo.CreateUserAsync(email,
            wrongLoginAttempts: 4);

        var wrongRequest = new LoginUserRequestDto
        {
            Email = email,
            Password = "WrongPassword!"
        };

        await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/login", wrongRequest);

        var user = await GetUserFromDbByEmailAsync(email);
        user!.Status.Should().Be(UserStatus.Blocked);

        var correctRequest = new LoginUserRequestDto
        {
            Email = email,
            Password = "TestPass123!"
        };

        var response = await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/login", correctRequest);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.UserIsBlocked);
    }

    /// <summary>
    /// Verifies that a successful login resets the LoginAttempts counter back to zero.
    /// </summary>
    [Fact]
    public async Task Login_Success_ResetsLoginAttempts()
    {
        var email = "reset-attempts@test.com";
        await UserRepo.CreateUserAsync(email);

        var wrongRequest = new LoginUserRequestDto
        {
            Email = email,
            Password = "Wrong!"
        };

        var correctRequest = new LoginUserRequestDto
        {
            Email = email,
            Password = "TestPass123!"
        };

        await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/login", wrongRequest);
        await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/login", correctRequest);

        var user = await GetUserFromDbByEmailAsync(email);
        user!.LoginAttempts.Should().Be(0);
    }

    #endregion

    #region SendNewResetPassword

    /// <summary>
    /// Verifies that an active user requesting a password reset receives 204 No Content,
    /// triggers one reset email via Brevo, and gets a reset token persisted in the database.
    /// </summary>
    [Fact]
    public async Task SendNewResetPasswordEmail_ActiveUser_Returns204AndEmailSentAndTokenCreated()
    {
        await UserRepo.CreateUserAsync("reset-req@test.com");

        Fixture.BrevoMock
            .Setup(b => b.SendResetPasswordEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var request = new SendNewResetPasswordEmailRequestDto { Email = "reset-req@test.com" };

        var response = await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/new/reset-password", request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        Fixture.BrevoMock.Verify(
            b => b.SendResetPasswordEmail(
                It.IsAny<string>(), "reset-req@test.com", It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);

        var user = await GetUserFromDbByEmailAsync("reset-req@test.com");
        user!.ResetPasswordToken.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that requesting a password reset for an unknown email returns 400
    /// with the <see cref="ErrorCode.NoUserWithThisEmail"/> error code.
    /// </summary>
    [Fact]
    public async Task SendNewResetPasswordEmail_UnknownEmail_Returns400WithNoUserWithThisEmail()
    {
        var request = new SendNewResetPasswordEmailRequestDto { Email = "nobody@test.com" };

        var response = await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/new/reset-password", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoUserWithThisEmail);
    }

    /// <summary>
    /// Verifies that a pending user cannot request a password reset and receives 400
    /// with the <see cref="ErrorCode.UserPending"/> error code.
    /// </summary>
    [Fact]
    public async Task SendNewResetPasswordEmail_PendingUser_Returns400WithUserPending()
    {
        await UserRepo.CreateUserAsync("pending-reset@test.com", status: UserStatus.Pending);

        var request = new SendNewResetPasswordEmailRequestDto { Email = "pending-reset@test.com" };

        var response = await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/new/reset-password", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.UserPending);
    }

    #endregion

    #region ResetPassword

    /// <summary>
    /// Verifies that a valid reset token allows changing the password, after which the old password
    /// no longer works and the new password grants access.
    /// </summary>
    [Fact]
    public async Task ResetPassword_ValidToken_Returns200AndCanLoginWithNewPassword()
    {
        var user = await UserRepo.CreateUserAsync("reset-ok@test.com", withResetToken: true);
        var resetToken = user.ResetPasswordToken!.Token;

        Fixture.BrevoMock
            .Setup(b => b.SendConfirmationResetPasswordEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var request = new ResetPasswordRequestDto
        {
            Password = "NewPass456!",
            RepeatedPassword = "NewPass456!"
        };

        var response = await Client.PostAsJsonAsync(
            $"{Routings.SystemAuthRouting}/reset-password?token={resetToken}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var oldLoginRequest = new LoginUserRequestDto
        {
            Email = "reset-ok@test.com",
            Password = "TestPass123!"
        };

        var oldLoginResponse = await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/login", oldLoginRequest);
        oldLoginResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var newLoginRequest = new LoginUserRequestDto
        {
            Email = "reset-ok@test.com",
            Password = "NewPass456!"
        };

        var newLoginResponse = await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/login", newLoginRequest);
        newLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Verifies that a successful password reset removes the reset token from the database.
    /// </summary>
    [Fact]
    public async Task ResetPassword_ValidToken_RemovesResetTokenFromDb()
    {
        var user = await UserRepo.CreateUserAsync("reset-token-remove@test.com", withResetToken: true);
        var resetToken = user.ResetPasswordToken!.Token;

        Fixture.BrevoMock
            .Setup(b => b.SendConfirmationResetPasswordEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var request = new ResetPasswordRequestDto { Password = "NewPass456!", RepeatedPassword = "NewPass456!" };

        await Client.PostAsJsonAsync(
            $"{Routings.SystemAuthRouting}/reset-password?token={resetToken}", request);

        var updated = await GetUserFromDbByEmailAsync("reset-token-remove@test.com");
        updated!.ResetPasswordToken.Should().BeNull();
    }

    /// <summary>
    /// Verifies that a successful password reset sends exactly one confirmation email via Brevo.
    /// </summary>
    [Fact]
    public async Task ResetPassword_ValidToken_SendsConfirmationEmail()
    {
        var user = await UserRepo.CreateUserAsync("reset-confirm-email@test.com", withResetToken: true);
        var resetToken = user.ResetPasswordToken!.Token;

        Fixture.BrevoMock
            .Setup(b => b.SendConfirmationResetPasswordEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var request = new ResetPasswordRequestDto { Password = "NewPass456!", RepeatedPassword = "NewPass456!" };

        await Client.PostAsJsonAsync(
            $"{Routings.SystemAuthRouting}/reset-password?token={resetToken}", request);

        Fixture.BrevoMock.Verify(
            b => b.SendConfirmationResetPasswordEmail(
                It.IsAny<string>(), It.IsAny<string>(), "reset-confirm-email@test.com"),
            Times.Once);
    }

    /// <summary>
    /// Verifies that a non-existent reset token returns 400
    /// with the <see cref="ErrorCode.InvalidResetPasswordToken"/> error code.
    /// </summary>
    [Fact]
    public async Task ResetPassword_InvalidToken_Returns400WithInvalidResetPasswordToken()
    {
        var request = new ResetPasswordRequestDto { Password = "NewPass456!", RepeatedPassword = "NewPass456!" };

        var response = await Client.PostAsJsonAsync(
            $"{Routings.SystemAuthRouting}/reset-password?token=invalidtoken", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.InvalidResetPasswordToken);
    }

    /// <summary>
    /// Verifies that mismatched password and repeated password return 400
    /// with the <see cref="ErrorCode.PasswordNotTheSame"/> error code.
    /// </summary>
    [Fact]
    public async Task ResetPassword_PasswordsDoNotMatch_Returns400WithPasswordNotTheSame()
    {
        var user = await UserRepo.CreateUserAsync("reset-mismatch@test.com", withResetToken: true);
        var resetToken = user.ResetPasswordToken!.Token;

        var request = new ResetPasswordRequestDto
        {
            Password = "NewPass456!",
            RepeatedPassword = "DifferentPass789!"
        };

        var response = await Client.PostAsJsonAsync(
            $"{Routings.SystemAuthRouting}/reset-password?token={resetToken}", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.PasswordNotTheSame);
    }

    /// <summary>
    /// Verifies that a new password that fails the strength rules returns 400
    /// with the <see cref="ErrorCode.PasswordTooShort"/> error code.
    /// </summary>
    [Fact]
    public async Task ResetPassword_WeakNewPassword_Returns400WithPasswordTooShort()
    {
        var user = await UserRepo.CreateUserAsync("reset-weak@test.com", withResetToken: true);
        var resetToken = user.ResetPasswordToken!.Token;

        var request = new ResetPasswordRequestDto { Password = "Ts1!", RepeatedPassword = "Ts1!" };

        var response = await Client.PostAsJsonAsync(
            $"{Routings.SystemAuthRouting}/reset-password?token={resetToken}", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.PasswordTooShort);
    }

    /// <summary>
    /// Verifies that a successful password reset clears the LoginAttempts counter
    /// and keeps the user in Active status.
    /// </summary>
    [Fact]
    public async Task ResetPassword_ResetsLoginAttemptsAndKeepsActiveStatus()
    {
        var email = "reset-attempts2@test.com";
        var user = await UserRepo.CreateUserAsync(email, withResetToken: true);
        var resetToken = user.ResetPasswordToken!.Token;

        Fixture.BrevoMock
            .Setup(b => b.SendConfirmationResetPasswordEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var loginRequest = new LoginUserRequestDto { Email = email, Password = "Wrong!" };
        var resetRequest = new ResetPasswordRequestDto { Password = "NewPass456!", RepeatedPassword = "NewPass456!" };

        await Client.PostAsJsonAsync($"{Routings.SystemAuthRouting}/login", loginRequest);
        await Client.PostAsJsonAsync(
            $"{Routings.SystemAuthRouting}/reset-password?token={resetToken}", resetRequest);

        var updated = await GetUserFromDbByEmailAsync(email);
        updated!.LoginAttempts.Should().Be(0);
        updated.Status.Should().Be(UserStatus.Active);
    }

    #endregion
}
