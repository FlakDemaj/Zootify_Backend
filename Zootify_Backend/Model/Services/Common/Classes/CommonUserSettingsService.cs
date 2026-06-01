using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Services.Common.Interfaces;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Model.Services.Common.Classes;

public class CommonUserSettingsService : ICommonUserSettingsService
{
    private readonly IUserSettingsRepository _userSettingsRepository;

    public CommonUserSettingsService(IUserSettingsRepository userSettingsRepository)
    {
        _userSettingsRepository = userSettingsRepository;
    }

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
