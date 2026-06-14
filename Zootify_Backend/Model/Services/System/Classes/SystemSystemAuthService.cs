using AutoMapper;
using Zootify_Backend.Configuration;
using Zootify_Backend.DTOs.System.Request;
using Zootify_Backend.DTOs.System.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Providers.Brevo;
using Zootify_Backend.Model.Services.System.Interfaces;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Model.Services.System.Classes;

/// <summary>
/// Handles all user-related business logic including registration, login, and password management.
/// </summary>
public class SystemSystemAuthService : ISystemAuthService
{
    private readonly IAuthRepository _authRepository;
    
    private readonly IBrevoProvider _brevoProvider;
   
    private readonly ITokenManager _tokenManager;
    
    private readonly IMapper _mapper;

    private IMapper Mapper { get; }

    /// <summary>
    /// Initializes a new <see cref="SystemSystemAuthService"/> with its required dependencies.
    /// </summary>
    /// <param name="authRepository">Repository for user persistence and lookup operations.</param>
    /// <param name="mapper">AutoMapper instance for object mapping.</param>
    /// <param name="brevoProvider">Email provider for sending transactional emails.</param>
    /// <param name="tokenManager">Manager for generating JWT access tokens.</param>
    public SystemSystemAuthService(
        IAuthRepository authRepository,
        IMapper mapper,
        IBrevoProvider brevoProvider,
        ITokenManager tokenManager)
    {
        _authRepository = authRepository;
        _mapper = mapper;
        Mapper = mapper;
        _brevoProvider = brevoProvider;
        _tokenManager = tokenManager;
    }

    #region Registration

    /// <summary>
    /// Validates the request, maps it to a <see cref="UserEntity"/>, hashes the password,
    /// persists the user, and sends a registration confirmation email.
    /// </summary>
    /// <param name="request">The registration data including email, name, and password.</param>
    /// <exception cref="ZootifyException">Thrown when a user with the given email already exists or the email/password is invalid.</exception>
    [Transaction(ReadOnly = false)]
    public async Task RegisterAsync(
        RegisterUserRequestDto request)
    {
        var existingUser = await
            _authRepository
                .GetUserByEmailWithSettingsAsync(request.Email.ToLower());

        if (existingUser != null)
        {
            throw new ZootifyException(ErrorCode.UserWithThisEmailAlreadyExists);
        }

        HelperMethods.CheckEmail(request.Email.ToLower());

        var user = Mapper.Map<UserEntity>(request);

        user.FormatEmail();
        CheckPassword(request.Password);
        user.SetPassword(request.Password);
        
        user.CreateUserSettings();

        await CreateUserAsync(user);
    }

    /// <summary>
    /// Looks up the user by the registration token, validates the token, activates the account,
    /// and removes the consumed token from the database.
    /// </summary>
    /// <param name="token">The registration token sent to the user by email.</param>
    /// <exception cref="ZootifyException">Thrown when the token is invalid, expired, or the user is already active, blocked, or deleted.</exception>
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
        user.SetUpdatedBy(user.Guid);
        user.RemoveRegistrationToken();

        await _authRepository.UpdateAsync(user);
    }

    /// <summary>
    /// Replaces the existing registration token with a new one and resends the confirmation email
    /// to the pending user.
    /// </summary>
    /// <param name="request">Contains the email address of the pending user to resend the confirmation to.</param>
    /// <exception cref="ZootifyException">Thrown when no user with the given email is found or the user is already active.</exception>
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

        // Auth emails follow the request's Accept-Language header.
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
    /// <param name="request">The login credentials including email and password.</param>
    /// <returns>A DTO containing the user's name, JWT token, and settings.</returns>
    /// <exception cref="ZootifyException">Thrown when the user is not found, is not active, or provides incorrect credentials.</exception>
    [NoTransaction]
    public async Task<LoginUserResponseDto> LoginAsync(
        LoginUserRequestDto request)
    {
        var user = await _authRepository.GetUserByEmailWithSettingsAsync(
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

        return _mapper.Map<LoginUserResponseDto>((user, token));
    }

    /// <summary>
    /// Validates the reset token, checks that the two passwords match,
    /// applies password-strength rules, sets the new password, resets login attempts,
    /// removes the token, and sends a confirmation email.
    /// </summary>
    /// <param name="request">The new password data including the password and its confirmation.</param>
    /// <param name="resetPasswordToken">The reset token sent to the user by email.</param>
    /// <exception cref="ZootifyException">Thrown when the token is invalid or expired, the passwords do not match, or the password does not meet strength requirements.</exception>
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
        user.SetUpdatedBy(user.Guid);
        user.ResetLoginAttempts();
        user.ActivateUser();
        user.RemoveResetPasswordToken();

        await _authRepository.UpdateAsync(user);

        // Auth emails follow the request's Accept-Language header.
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
    /// <param name="request">Contains the email address of the user requesting a password reset.</param>
    /// <exception cref="ZootifyException">Thrown when no user with the given email is found, or the user is pending or deleted.</exception>
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

        // Auth emails follow the request's Accept-Language header.
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
    /// <param name="user">The user entity to persist and send the confirmation email to.</param>
    /// <exception cref="ZootifyException">Thrown when the confirmation email could not be sent.</exception>
    private async Task CreateUserAsync(UserEntity user)
    {
        var registrationToken = user
            .CreateRegistrationToken(ZootifyConfiguration.RegistrationTokenExpiryInHours);

        var createdUser = await _authRepository.CreateAsync(user);

        // Auth emails follow the request's Accept-Language header (the user has no settings yet or
        // is not authenticated), so no per-user culture override is applied here.
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
    /// <param name="password">The plain-text password to validate.</param>
    /// <exception cref="ZootifyException">Thrown when the password does not meet the strength requirements.</exception>
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

    #endregion
}
