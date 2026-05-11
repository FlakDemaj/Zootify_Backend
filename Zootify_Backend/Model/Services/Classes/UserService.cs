using AutoMapper;
using Zootify_Backend.Configuration;
using Zootify_Backend.DTOs.Request;
using Zootify_Backend.DTOs.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Services.Interfaces;
using Zootify_Backend.Models.Providers;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Model.Services.Classes;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IBrevoProvider _brevoProvider;
    private readonly ITokenManager _tokenManager;
    
    private IMapper Mapper { get; }

    public UserService(
        IUserRepository userRepository,
        IMapper mapper,
        IBrevoProvider brevoProvider,
        ITokenManager tokenManager)
    {
        _userRepository = userRepository;
        Mapper = mapper;
        _brevoProvider = brevoProvider;
        _tokenManager = tokenManager;
    }
    
    #region Registration

    [Transaction(ReadOnly = false)]
    public async Task RegisterAsync(
        RegisterUserRequestDto request)
    {
        var existingUser = await 
            _userRepository
                .GetUserByEmail(request.Email.ToLower());

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

    [Transaction(ReadOnly = false)]
    public async Task ConfirmRegistrationAsync(string token)
    {
        var user = await _userRepository.GetUserByRegistrationTokenAsync(token);
        
        if (user == null)
        {
            throw new ZootifyException(ErrorCode.NoUserWithThisId);
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

        await _userRepository.UpdateAsync(user);
    }

    [Transaction(ReadOnly = false)]
    public async Task SendNewRegistrationEmailAsync(
        SendNewRegistrationEmailRequestDto request)
    {
        var user = await _userRepository
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
        
        await _userRepository.UpdateAsync(user);
        
        await _brevoProvider.SendRegistrationEmail(
            registrationToken.Token,
            user.Email,
            user.FirstName,
            user.LastName);
    }

    #endregion

    #region Login

    // No Transaction because of the login
    public async Task<LoginUserResponseDto> LoginAsync(
        LoginUserRequestDto request)
    {
        var user = await _userRepository.GetUserByEmail(
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
            user.IncrementLoginAttempts();
            await _userRepository.UpdateAsync(user);
            throw new ZootifyException(ErrorCode.IncorrectPassword);
        }
        
        user.ResetLoginAttempts();
        await _userRepository.UpdateAsync(user);

        return _tokenManager.GenerateAccessToken(
            user.Guid,
            user.Role);
    }
    
    [Transaction(ReadOnly = false)]
    public async Task ResetPasswordAsync(
        ResetPasswordRequestDto request,
        string resetPasswordToken)
    {
        var user = await _userRepository.GetUserByResetPasswordTokenWithResetPassword(
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
        user.RemoveResetPasswordToken();
        
        await _userRepository.UpdateAsync(user);
        
        await _brevoProvider.SendConfirmationResetPasswordEmail(
            user.FirstName,
            user.LastName,
            user.Email);
    }
    
    [Transaction(ReadOnly = false)]
    public async Task SendNewResetPasswordEmailAsync(SendNewResetPasswordEmailRequestDto request)
    {
        var user = await _userRepository.GetUserByEmailWithResetPassword(
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
        
        await _userRepository.UpdateAsync(user);
        
        await _brevoProvider.SendResetPasswordEmail(
            resetPasswordToken.Token,
            user.Email,
            user.FirstName,
            user.LastName);
    }

    #endregion

    #region PrivateMethods

    private async Task CreateUserAsync(UserEntity user)
    {
        var registrationToken = user
            .CreateRegistrationToken(ZootifyConfiguration.RegistrationTokenExpiryInHours);
        
        var createdUser = await _userRepository.CreateAsync(user);
        
        await _brevoProvider.SendRegistrationEmail(
            registrationToken.Token,
            createdUser.Email,
            createdUser.FirstName,
            createdUser.LastName);
    }

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