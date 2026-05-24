using AutoMapper;
using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Common.Interfaces;
using Zootify_Backend.Model.Services.Admin.Interfaces;
using Zootify_Backend.Model.Services.Common.Interfaces;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Model.Services.Admin.Classes;

public class AdminUserService : IAdminUserServices
{
    private readonly IUserRepository _userRepository;
    
    private readonly ICommonUserService _commonUserService;
    
    private readonly IMapper _mapper;

    public AdminUserService(IUserRepository userRepository, IMapper mapper, ICommonUserService commonUserService)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _commonUserService = commonUserService;
    }

    [Transaction(ReadOnly = false)]
    public async Task UpdateUserAsync(
        IAuthentication authentication,
        Guid userToUpdateGuid,
        UpdateUserRequestDto updateUserRequestDto)
    {

        if (authentication.UserGuid == userToUpdateGuid)
        {
            throw new ZootifyException(ErrorCode.Forbidden);
        }
        
        var requester =  await _userRepository
            .GetActiveUserByGuid(authentication.UserGuid);

        if (requester == null)
        {
            throw new ZootifyException(ErrorCode.NoUserWithThisGuid);
        }
        
        var oldUser = await _userRepository
            .GetActiveUserByGuid(userToUpdateGuid);

        if (oldUser == null)
        {
            throw new ZootifyException(ErrorCode.NoUserWithThisGuid);
        }
        
        var mappedUser = _mapper.Map<UserEntity>(updateUserRequestDto);
        mappedUser.Email = oldUser.Email;
        
        await _commonUserService.UpdateUserAsync(
            mappedUser,
            oldUser,
            requester.Guid);
    }
    
    [Transaction(ReadOnly = false)]
    public async Task ActivateUserAsync(
        IAuthentication authentication,
        Guid userToDeleteGuid)
    {
        var userToActivate = await _userRepository
            .GetDeletedUserByGuid(userToDeleteGuid);

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