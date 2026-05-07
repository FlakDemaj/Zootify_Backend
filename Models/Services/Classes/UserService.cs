using AutoMapper;
using Zootify_Backend.Configuration;
using Zootify_Backend.DTOs.Request;
using Zootify_Backend.DTOs.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Models.DataAccess.Entitys;
using Zootify_Backend.Models.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Models.Providers;
using Zootify_Backend.Models.Services.Interfaces;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Models.Services.Classes;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IBrevoProvider _brevoProvider;
    private IMapper Mapper { get; }

    public UserService(
        IUserRepository userRepository,
        IMapper mapper,
        IBrevoProvider brevoProvider)
    {
        _userRepository = userRepository;
        Mapper = mapper;
        _brevoProvider = brevoProvider;
    }

    [Transaction(ReadOnly = false)]
    public async Task<RegisterUserResponseDto> RegisterAsync(
        RegisterUserRequestDto registerUserRequestDto)
    {
        var existingUser = await 
            _userRepository
                .GetUserByEmail(registerUserRequestDto.Email);

        if (existingUser != null)
        {
            throw new ZootifyException(ErrorCode.UserWithThisEmailAlreadyExists);
        }

        var user = Mapper.Map<UserEntity>(registerUserRequestDto);

        user.CheckEmail();
        CheckPassword(registerUserRequestDto.Password);
        user.HashPassword(registerUserRequestDto.Password);

        return await CreateUserAsync(user);
    }

    [Transaction(ReadOnly = false)]
    public async Task<LoginUserResponseDto> ConfirmRegistrationAsync(string token)
    {
        var user = await _userRepository.GetUserByRegistrationTokenAsync(token);
        
        if (user == null)
        {
            throw new ZootifyException(ErrorCode.NoUserWithThisId);
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

        user.ConfirmUser();
        user.SetUpdatedBy(null);
        user.RemoveRegistrationToken(registrationToken);

        await _userRepository.UpdateAsync(user);
        
        return new LoginUserResponseDto
        {
            Token = "test"
        };
    }

    public async Task<SendNewRegistrationEmailResponseDto> SendNewRegistrationEmailAsync(
        SendNewRegistrationEmailRequestDto request)
    {
        var user = await _userRepository.GetUserByEmail(request.Email);

        if (user == null)
        {
            throw new ZootifyException(ErrorCode.NoUserWithTHisEmail);
        }

        if (user.Status == UserStatus.Active)
        {
            throw new ZootifyException(ErrorCode.DatabaseError);
        }

        var registrationToken = user.CreateRegistrationToken(ZootifyConfiguration.RegistrationTokenExpiryInHours);
        
        await _userRepository.UpdateAsync(user);
        
        var emailSent = await _brevoProvider.SendRegistrationEmail(
            registrationToken.Token,
            user.Email,
            user.FirstName,
            user.LastName);

        if (!emailSent)
        {
            throw new ZootifyException(ErrorCode.DefaultErrorCode);
        }

        return new SendNewRegistrationEmailResponseDto
        {
            IsCreated = true
        };
    }

    private async Task<RegisterUserResponseDto> CreateUserAsync(UserEntity user)
    {
        var registrationToken = user
            .CreateRegistrationToken(ZootifyConfiguration.RegistrationTokenExpiryInHours);
        
        var createdUser = await _userRepository.CreateAsync(user);
        
        var emailSent = await _brevoProvider.SendRegistrationEmail(
            registrationToken.Token,
            createdUser.Email,
            createdUser.FirstName,
            createdUser.LastName);

        if (!emailSent)
        {
            throw new ZootifyException(ErrorCode.DefaultErrorCode);
        }

        return new RegisterUserResponseDto
        {
            IsCreated = true
        };
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
}