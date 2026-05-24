using AutoMapper;
using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Common.Interfaces;
using Zootify_Backend.Model.Services.Common.Interfaces;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Model.Services.Common.Classes;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    
    private readonly IMapper _mapper;

    public UserService(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    [Transaction(ReadOnly = false)]
    public async Task<GetUserResponseDto> GetUserByGuidAsync(
        IAuthentication authentication,
        Guid guid)
    {
        if (authentication.Role == UserRole.User)
        {
            throw new ZootifyException(ErrorCode.Forbidden);
        }
        
        var user = await _userRepository.GetUserByGuidWithAuditInformationAsync(guid);

        if (user == null)
        {
            throw new ZootifyException(ErrorCode.NoUserWithThisGuid);
        }
        
        return _mapper.Map<GetUserResponseDto>(user);
    }

    [Transaction(ReadOnly = false)]
    public async Task UpdateUserAsync(
        IAuthentication authentication,
        Guid userToUpdateGuid,
        UpdateUserRequestDto updateUserRequestDto)
    {
        if (authentication.Role != UserRole.Admin
            || authentication.UserGuid == userToUpdateGuid)
        {
            throw new ZootifyException(ErrorCode.Forbidden);
        }
        
        var requester =  await _userRepository
            .GetUserByGuid(authentication.UserGuid);

        if (requester == null)
        {
            throw new ZootifyException(ErrorCode.NoUserWithThisGuid);
        }
        
        var oldUser = await _userRepository
            .GetUserByGuidWithAuditInformationAsync(userToUpdateGuid);

        if (oldUser == null)
        {
            throw new ZootifyException(ErrorCode.NoUserWithThisGuid);
        }
        
        var mappedUser = _mapper.Map<UserEntity>(updateUserRequestDto);
        mappedUser.Email = oldUser.Email;
        
        await UpdateUserAsync(
            mappedUser,
            oldUser,
            requester.Guid);
    }
    
    [Transaction(ReadOnly = false)]
    public async Task UpdateOwnUserAsync(
        IAuthentication authentication,
        UpdateOwnUserRequestDto updateOwnUserRequestDto)
    {
        var requester =  await _userRepository
            .GetUserByGuid(authentication.UserGuid);

        if (requester == null)
        {
            throw new ZootifyException(ErrorCode.NoUserWithThisGuid);
        }
        
        HelperMethods.CheckEmail(updateOwnUserRequestDto.Email);
        
        var mappedUser = _mapper.Map<UserEntity>(updateOwnUserRequestDto);
        
        mappedUser.FormatEmail();
        
        await UpdateUserAsync(
            mappedUser,
            requester,
            authentication.UserGuid);
    }

    private async Task UpdateUserAsync(
        UserEntity newUser,
        UserEntity oldUser,
        Guid? requesterGuid)
    {

        if (!CheckUserAttributes(oldUser, newUser))
        {
            throw new ZootifyException(ErrorCode.NoUpdatedData);
        }
        
        oldUser.FirstName = newUser.FirstName;
        oldUser.LastName = newUser.LastName;
        oldUser.Role = newUser.Role;
        oldUser.Email = newUser.Email;
        oldUser.SetUpdatedBy(requesterGuid);
        
        await _userRepository.UpdateAsync(oldUser);
    }

    private bool CheckUserAttributes(
        UserEntity oldUser,
        UserEntity newUser)
    {
        return oldUser.FirstName != newUser.FirstName
               || oldUser.LastName != newUser.LastName
               || oldUser.Email != newUser.Email
               || oldUser.Role != newUser.Role;
    }
    
}