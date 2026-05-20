using AutoMapper;
using Zootify_Backend.Configuration;
using Zootify_Backend.DTOs.Auth.Request;
using Zootify_Backend.DTOs.Auth.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Services.System.Interfaces;
using Zootify_Backend.Models.Providers;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Model.Services.System.Classes;

/// <summary>
/// Handles all user-related business logic including registration, login, and password management.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepository;
    private readonly IBrevoProvider _brevoProvider;
    private readonly ITokenManager _tokenManager;

    private IMapper Mapper { get; }

    /// <summary>
    /// Initializes a new <see cref="AuthService"/> with its required dependencies.
    /// </summary>
    public AuthService(
        IAuthRepository authRepository,
        IMapper mapper,
        IBrevoProvider brevoProvider,
        ITokenManager tokenManager)
    {
        _authRepository = authRepository;
        Mapper = mapper;
        _brevoProvider = brevoProvider;
        _tokenManager = tokenManager;
    }

    #region Registration

    /// <summary>
    /// Validates the request, maps it to a <see cref="UserEntity"/>, hashes the password,
    /// persists the user, and sends a registration confirmation email.
    /// </summary>
    [Transaction(ReadOnly = false)]
    public async Task RegisterAsync(
        RegisterUserRequestDto request)
    {
        var existingUser = await
            _authRepository
                .GetUserByEmailAsync(request.Email.ToLower());

        if (existingUser != null)
        {
            throw new ZootifyException(ErrorCode.UserWithThisEmailAlreadyExists);
        }

        CheckEmail(request.Email.ToLower());

        var user = Mapper.Map<UserEntity>(request);

        user.FormatEmail();
        CheckPassword(request.Password);
        user.SetPassword(request.Password);

        await CreateUserAsync(user);
    }

    /// <summary>
    /// Looks up the user by the registration token, validates the token, activates the account,
    /// and removes the consumed token from the database.
    /// </summary>
    [Transaction(ReadOnly = false)]
    public async Task ConfirmRegistrationAsync(string token)
    {
        var user = await _authRepository.GetUserByRegistrationTokenAsync(token);

        if (user == null)
        {
            throw new ZootifyException(ErrorCode.NoUserWithThisToken);
        }

        switch (user.Status)
        {
            case UserStatus.Active:
                throw new ZootifyException(ErrorCode.UserAlreadyActive);
            case UserStatus.Blocked:
                throw new ZootifyException(ErrorCode.UserIsBlocked);
            case UserStatus.Deleted:
                throw new ZootifyException(ErrorCode.UserIsDeleted);
        }

        var registrationToken = user.RegistrationToken;

        if (registrationToken == null)
        {
            throw new ZootifyException(ErrorCode.InvalidRegistrationToken);
        }

        if (registrationToken.IsExpired())
        {
            throw new ZootifyException(ErrorCode.RegistrationTokenExpired);
        }

        user.ActivateUser();
        user.SetUpdatedBy(null);
        user.RemoveRegistrationToken();

        await _authRepository.UpdateAsync(user);
    }

    /// <summary>
    /// Replaces the existing registration token with a new one and resends the confirmation email
    /// to the pending user.
    /// </summary>
    [Transaction(ReadOnly = false)]
    public async Task SendNewRegistrationEmailAsync(
        SendNewRegistrationEmailRequestDto request)
    {
        var user = await _authRepository
            .GetUserByEmailWithRegistrationToken(request.Email.ToLower());

        if (user == null)
        {
            throw new ZootifyException(ErrorCode.NoUserWithThisEmail);
        }

        if (user.Status == UserStatus.Active)
        {
            throw new ZootifyException(ErrorCode.DatabaseError);
        }

        var registrationToken = user
            .CreateRegistrationToken(ZootifyConfiguration.RegistrationTokenExpiryInHours);

        await _authRepository.UpdateAsync(user);

        var wasSent = await _brevoProvider.SendRegistrationEmail(
            registrationToken.Token,
            user.Email,
            user.FirstName,
            user.LastName);

        if (!wasSent)
        {
            throw new ZootifyException(ErrorCode.DefaultErrorCode);
        }
    }

    #endregion

    #region Login

    // No Transaction because login intentionally updates LoginAttempts outside a wrapping transaction.
    /// <summary>
    /// Validates the user's credentials, tracks failed attempts (blocking after 5),
    /// resets the counter on success, and returns a signed JWT token.
    /// </summary>
    public async Task<LoginUserResponseDto> LoginAsync(
        LoginUserRequestDto request)
    {
        var user = await _authRepository.GetUserByEmailAsync(
            request.Email.ToLower(),
            true);

        if (user == null)
        {
            throw new ZootifyException(ErrorCode.NoUserWithThisEmail);
        }

        switch (user.Status)
        {
            case UserStatus.Pending:
                throw new ZootifyException(ErrorCode.UserPending);
            case UserStatus.Blocked:
                throw new ZootifyException(ErrorCode.UserIsBlocked);
            case UserStatus.Deleted:
                throw new ZootifyException(ErrorCode.UserIsDeleted);
        }

        if (!user.CheckPassword(request.Password))
        {
            user.IncrementLoginAttempts(ZootifyConfiguration.MaximumLoginAttempts);
            await _authRepository.UpdateAsync(user);
            throw new ZootifyException(ErrorCode.IncorrectPassword);
        }

        user.ResetLoginAttempts();
        await _authRepository.UpdateAsync(user);

        var token = _tokenManager.GenerateAccessToken(
            user.Guid,
            user.Role);

        return new LoginUserResponseDto
        {
            Token = token,
            Name = user.FirstName + " " + user.LastName
        };
    }

    /// <summary>
    /// Validates the reset token, checks that the two passwords match,
    /// applies password-strength rules, sets the new password, resets login attempts,
    /// removes the token, and sends a confirmation email.
    /// </summary>
    [Transaction(ReadOnly = false)]
    public async Task ResetPasswordAsync(
        ResetPasswordRequestDto request,
        string resetPasswordToken)
    {
        var user = await _authRepository.GetUserByResetPasswordTokenWithResetPassword(
            resetPasswordToken);

        if (user == null)
        {
            throw new ZootifyException(ErrorCode.InvalidResetPasswordToken);
        }

        var resetPasswordTokenEntity = user.ResetPasswordToken;

        if (resetPasswordTokenEntity == null)
        {
            throw new ZootifyException(ErrorCode.InvalidResetPasswordToken);
        }

        if (resetPasswordTokenEntity.IsExpired())
        {
            throw new ZootifyException(ErrorCode.ResetPasswordTokenExpired);
        }

        if (request.Password != request.RepeatedPassword)
        {
            throw new ZootifyException(ErrorCode.PasswordNotTheSame);
        }

        CheckPassword(request.Password);

        user.SetPassword(request.RepeatedPassword);
        user.SetUpdatedBy(null);
        user.ResetLoginAttempts();
        user.ActivateUser();
        user.RemoveResetPasswordToken();

        await _authRepository.UpdateAsync(user);

        var wasSent = await _brevoProvider.SendConfirmationResetPasswordEmail(
            user.FirstName,
            user.LastName,
            user.Email);
        
        if (!wasSent)
        {
            throw new ZootifyException(ErrorCode.DefaultErrorCode);
        }
    }

    /// <summary>
    /// Creates a reset-password token, persists the updated user, and sends the reset email.
    /// </summary>
    [Transaction(ReadOnly = false)]
    public async Task SendNewResetPasswordEmailAsync(SendNewResetPasswordEmailRequestDto request)
    {
        var user = await _authRepository.GetUserByEmailWithResetPassword(
            request.Email.ToLower());

        if (user == null)
        {
            throw new ZootifyException(ErrorCode.NoUserWithThisEmail);
        }

        switch (user.Status)
        {
            case UserStatus.Pending:
                throw new ZootifyException(ErrorCode.UserPending);
            case UserStatus.Deleted:
                throw new ZootifyException(ErrorCode.UserIsDeleted);
        }

        var resetPasswordToken = user
            .CreateResetPasswordTokenEntityToken(ZootifyConfiguration.ResetPasswordTokenExpiryInHours);

        await _authRepository.UpdateAsync(user);

        var wasSent = await _brevoProvider.SendResetPasswordEmail(
            resetPasswordToken.Token,
            user.Email,
            user.FirstName,
            user.LastName);
        
        if (!wasSent)
        {
            throw new ZootifyException(ErrorCode.DefaultErrorCode);
        }
    }

    #endregion

    #region PrivateMethods

    /// <summary>
    /// Creates the registration token on the user, persists the user to the database,
    /// and sends the registration confirmation email via Brevo.
    /// </summary>
    private async Task CreateUserAsync(UserEntity user)
    {
        var registrationToken = user
            .CreateRegistrationToken(ZootifyConfiguration.RegistrationTokenExpiryInHours);

        var createdUser = await _authRepository.CreateAsync(user);

        var wasSent = await _brevoProvider.SendRegistrationEmail(
            registrationToken.Token,
            createdUser.Email,
            createdUser.FirstName,
            createdUser.LastName);

        if (!wasSent)
        {
            throw new ZootifyException(ErrorCode.DefaultErrorCode);
        }
        
    }

    /// <summary>
    /// Validates password strength: minimum length, uppercase, lowercase, digit, and special character.
    /// Throws a <see cref="ZootifyException"/> with the specific failing rule.
    /// </summary>
    private static void CheckPassword(string password)
    {
        if (password.Length < ZootifyConfiguration.MinimumPasswordLength)
        {
            throw new ZootifyException(ErrorCode.PasswordTooShort);
        }

        var hasUppercase = false;
        var hasLowercase = false;
        var hasDigit = false;
        var hasSpecialCharacter = false;

        foreach (var character in password)
        {
            if (char.IsUpper(character))
            {
                hasUppercase = true;
            }

            if (char.IsLower(character))
            {
                hasLowercase = true;
            }

            if (char.IsDigit(character))
            {
                hasDigit = true;
            }

            if (ZootifyConfiguration.SpecialCharacters.Contains(character))
            {
                hasSpecialCharacter = true;
            }
        }

        if (!hasUppercase)
        {
            throw new ZootifyException(ErrorCode.PasswordMissingUppercase);
        }

        if (!hasLowercase)
        {
            throw new ZootifyException(ErrorCode.PasswordMissingLowercase);
        }

        if (!hasDigit)
        {
            throw new ZootifyException(ErrorCode.PasswordMissingDigit);
        }

        if (!hasSpecialCharacter)
        {
            throw new ZootifyException(ErrorCode.PasswordMissingSpecialCharacter);
        }
    }

    /// <summary>
    /// Validates that the email contains "@" and ".", and meets the minimum length requirement.
    /// Throws a <see cref="ZootifyException"/> with <see cref="ErrorCode.EmailError"/> on failure.
    /// </summary>
    private void CheckEmail(string email)
    {
        if (!email.Contains("@")
            || !email.Contains(".")
            || email.Length < 7)
        {
            throw new ZootifyException(ErrorCode.EmailError);
        }
    }

    #endregion
}
