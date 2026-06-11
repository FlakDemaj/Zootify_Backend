using AutoMapper;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Services.Common.Interfaces;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Model.Services.Common.Classes;

public class CommonUserService : ICommonUserService
{
    private readonly IUserRepository _userRepository;
    
    private readonly IMapper _mapper;

    public CommonUserService(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    [Transaction(ReadOnly = false)]
    public async Task DeleteUserAsync(
        IAuthentication authentication,
        Guid userToDeleteGuid)
    {
        if (authentication.Role != UserRole.Admin
            && authentication.UserGuid != userToDeleteGuid)
        {
            throw new ZootifyException(ErrorCode.Forbidden);
        }
        
        var userToDelete = await _userRepository
            .GetActiveUserByGuid(userToDeleteGuid);

        if (userToDelete == null)
        {
            throw new ZootifyException(ErrorCode.NoUserWithThisGuid);
        }
        
        userToDelete.Status = UserStatus.Deleted;
        userToDelete.SetDeletedBy(authentication.UserGuid);
        
        await _userRepository.UpdateAsync(userToDelete);
    }
    
    [Transaction(ReadOnly = false)]
    public async Task UpdateOwnUserAsync(
        IAuthentication authentication,
        UpdateOwnUserRequestDto updateOwnUserRequestDto)
    {
        var requester =  await _userRepository
            .GetActiveUserByGuid(authentication.UserGuid);

        if (requester == null)
        {
            throw new ZootifyException(ErrorCode.NoUserWithThisGuid);
        }
        
        HelperMethods.CheckEmail(updateOwnUserRequestDto.Email);

        var mappedUser = _mapper.Map<UserEntity>(updateOwnUserRequestDto);

        mappedUser.FormatEmail();

        if (!requester.CheckChanges(mappedUser))
        {
            throw new ZootifyException(ErrorCode.NoUpdatedData);
        }

        requester.UpdateUserData(mappedUser, authentication.UserGuid);

        await _userRepository.UpdateAsync(requester);
    }

    [Transaction(ReadOnly = true)]
    public async Task<GetOwnUserData> GetOwnUserDataAsync(IAuthentication authentication)
    {
        var user = await _userRepository
            .GetActiveUserByGuid(authentication.UserGuid);

        if (user == null)
        {
            throw new ZootifyException(ErrorCode.NoUserWithThisGuid);
        }
        
        return _mapper.Map<GetOwnUserData>(user);
    }
    
}