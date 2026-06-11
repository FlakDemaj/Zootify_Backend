using AutoMapper;
using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.DTOs.Admin.Response;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Services.Admin.Interfaces;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Model.Services.Admin.Classes;

public class AdminModeratorUserService : IAdminModeratorUserService
{
    private readonly IUserRepository _userRepository;

    private readonly IMapper _mapper;

    public AdminModeratorUserService(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    [Transaction(ReadOnly = true)]
    public async Task<GetUserResponseDto> GetUserByGuidAsync(Guid guid)
    {
        var user = await _userRepository.GetUserByGuidWithAuditInformationAsync(guid);

        if (user == null)
        {
            throw new ZootifyException(ErrorCode.NoUserWithThisGuid);
        }

        return _mapper.Map<GetUserResponseDto>(user);
    }

    [Transaction(ReadOnly = true)]
    public async Task<List<GetAllUserResponseDto>> GetAllUsersAsync(GetAllUsersFilterDto query)
    {
        var users = await _userRepository.GetAllUsersAsync(query);
        return _mapper.Map<List<GetAllUserResponseDto>>(users);
    }

    [Transaction(ReadOnly = false)]
    public async Task UpdateUserAsync(
        IAuthentication authentication,
        Guid userToUpdateGuid,
        UpdateUserRequestDto updateUserRequestDto)
    {
        if (authentication.Role != UserRole.Admin)
        {
            throw new ZootifyException(ErrorCode.Forbidden);
        }

        if (authentication.UserGuid == userToUpdateGuid)
        {
            throw new ZootifyException(ErrorCode.Forbidden);
        }

        var requester = await _userRepository.GetActiveUserByGuid(authentication.UserGuid);

        if (requester == null)
        {
            throw new ZootifyException(ErrorCode.NoUserWithThisGuid);
        }

        var oldUser = await _userRepository.GetActiveUserByGuid(userToUpdateGuid);

        if (oldUser == null)
        {
            throw new ZootifyException(ErrorCode.NoUserWithThisGuid);
        }

        var mappedUser = _mapper.Map<UserEntity>(updateUserRequestDto);
        mappedUser.Email = oldUser.Email;

        if (!oldUser.CheckChanges(mappedUser))
        {
            throw new ZootifyException(ErrorCode.NoUpdatedData);
        }

        oldUser.UpdateUserData(mappedUser, requester.Guid);
        await _userRepository.UpdateAsync(oldUser);
    }

    [Transaction(ReadOnly = false)]
    public async Task ActivateUserAsync(
        IAuthentication authentication,
        Guid userToActivateGuid)
    {
        var userToActivate = await _userRepository.GetDeletedUserByGuid(userToActivateGuid);

        if (userToActivate == null)
        {
            throw new ZootifyException(ErrorCode.NoUserWithThisGuid);
        }

        userToActivate.Status = UserStatus.Active;
        userToActivate.RemoveDeletion();
        userToActivate.SetUpdatedBy(authentication.UserGuid);

        await _userRepository.UpdateAsync(userToActivate);
    }
}
