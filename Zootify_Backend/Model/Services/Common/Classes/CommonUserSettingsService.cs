using Zootify_Backend.DTOs.Common.Request;
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

    /// <summary>
    /// Initializes a new <see cref="CommonUserSettingsService"/> with its required repository.
    /// </summary>
    /// <param name="userSettingsRepository">Repository for user settings persistence operations.</param>
    public CommonUserSettingsService(IUserSettingsRepository userSettingsRepository)
    {
        _userSettingsRepository = userSettingsRepository;
    }

    /// <summary>
    /// Updates the authenticated user's personal settings after detecting changes.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="requestDto">The new settings values to apply.</param>
    [Transaction(ReadOnly = false)]
    public async Task UpdateUserSettingsAsync(
        IAuthentication authentication,
        UpdateUserSettingsRequestDto requestDto)
    {
        var settings = await _userSettingsRepository
            .GetByUserGuidAsync(authentication.UserGuid);

        if (settings == null)
        {
            throw new ZootifyException(ErrorCode.DefaultErrorCode);
        }

        if (!settings.CheckChanges(requestDto.DarkMode, requestDto.Language))
        {
            throw new ZootifyException(ErrorCode.NoUpdatedData);
        }

        settings.Apply(requestDto.DarkMode, requestDto.Language);

        await _userSettingsRepository.UpdateAsync(settings);
    }
}
