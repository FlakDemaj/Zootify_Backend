using AutoMapper;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Services.Common.Interfaces;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Model.Services.Common.Classes;

/// <summary>
/// Handles updates to a user's personal application settings such as dark mode and language preference.
/// </summary>
public class CommonUserSettingsService : ICommonUserSettingsService
{
    private readonly IUserSettingsRepository _userSettingsRepository;

    private readonly IUserRepository _userRepository;

    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="CommonUserSettingsService"/> with its required dependencies.
    /// </summary>
    /// <param name="userSettingsRepository">Repository for user settings persistence operations.</param>
    /// <param name="userRepository">Repository used to verify the requesting user is active.</param>
    /// <param name="mapper">AutoMapper instance for object mapping.</param>
    public CommonUserSettingsService(
        IUserSettingsRepository userSettingsRepository,
        IUserRepository userRepository,
        IMapper mapper)
    {
        _userSettingsRepository = userSettingsRepository;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    /// <summary>
    /// Throws <see cref="ErrorCode.ObjectIsNotActive"/> if the authenticated user is not active, so that
    /// blocked or deleted users cannot change their settings.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <exception cref="ZootifyException">Thrown when the requesting user is not active.</exception>
    private async Task EnsureRequesterIsActiveAsync(IAuthentication authentication)
    {
        var user = await _userRepository.GetActiveUserByGuid(authentication.UserGuid);
        if (user == null)
        {
            throw new ZootifyException(ErrorCode.ObjectIsNotActive);
        }
    }

    /// <summary>
    /// Returns the authenticated user's personal settings.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <returns>The user's personal settings.</returns>
    /// <exception cref="ZootifyException">Thrown when the user has no settings.</exception>
    [Transaction(ReadOnly = true)]
    public async Task<GetUserSettingsResponseDto> GetUserSettingsAsync(IAuthentication authentication)
    {
        var settings = await _userSettingsRepository
            .GetByUserGuidAsync(authentication.UserGuid);

        if (settings == null)
        {
            throw new ZootifyException(ErrorCode.UserSettingsNotFound);
        }

        return _mapper.Map<GetUserSettingsResponseDto>(settings);
    }

    /// <summary>
    /// Updates the authenticated user's personal settings after detecting changes.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="requestDto">The new settings values to apply.</param>
    /// <exception cref="ZootifyException">Thrown when the user has no settings.</exception>
    /// <exception cref="ZootifyException">Thrown when no changes are detected.</exception>
    [Transaction(ReadOnly = false)]
    public async Task UpdateUserSettingsAsync(
        IAuthentication authentication,
        UpdateUserSettingsRequestDto requestDto)
    {
        await EnsureRequesterIsActiveAsync(authentication);

        var settings = await _userSettingsRepository
            .GetByUserGuidAsync(authentication.UserGuid);

        if (settings == null)
        {
            throw new ZootifyException(ErrorCode.UserSettingsNotFound);
        }

        if (!requestDto.SendEmail && !requestDto.SendPushNotification)
        {
            throw new ZootifyException(ErrorCode.OneNotificationNeedsToBeActivated);
        }

        if (!settings.CheckChanges(
                requestDto.DarkMode, 
                requestDto.SendEmail,
                requestDto.SendPushNotification,
                requestDto.Language))
        {
            throw new ZootifyException(ErrorCode.NoUpdatedData);
        }

        settings.Apply(
            requestDto.SendPushNotification,
            requestDto.SendEmail,
            requestDto.DarkMode,
            requestDto.Language);

        await _userSettingsRepository.UpdateAsync(settings);
    }

    /// <summary>
    /// Updates the authenticated user's personal settings language.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="requestDto">The new language values to apply.</param>
    /// <exception cref="ZootifyException">Thrown when the user has no settings.</exception>
    /// <exception cref="ZootifyException">Thrown when no changes are detected.</exception>
    public async Task UpdateUserSettingsLanguageAsync(IAuthentication authentication, UpdateUserSettingsLanguageRequestDto requestDto)
    {
        await EnsureRequesterIsActiveAsync(authentication);

        var settings = await _userSettingsRepository
            .GetByUserGuidAsync(authentication.UserGuid);

        if (settings == null)
        {
            throw new ZootifyException(ErrorCode.UserSettingsNotFound);
        }

        if (settings.Language == requestDto.Language)
        {
            throw new ZootifyException(ErrorCode.NoUpdatedData);
        }
        
        settings.Apply(
            settings.SendPushNotification,
            settings.SendEmail,
            settings.DarkMode,
            requestDto.Language);
        
        await _userSettingsRepository.UpdateAsync(settings);
    }
}
