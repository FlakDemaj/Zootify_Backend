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

/// <summary>
/// Handles self-service user operations available to all authenticated roles.
/// </summary>
public class CommonUserService : ICommonUserService
{
    private readonly IUserRepository _userRepository;

    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="CommonUserService"/> with its required dependencies.
    /// </summary>
    /// <param name="userRepository">Repository for user persistence operations.</param>
    /// <param name="mapper">AutoMapper instance for object mapping.</param>
    public CommonUserService(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    /// <summary>
    /// Soft-deletes a user account. Anyone may delete themselves; a moderator may additionally delete
    /// regular users; an admin may delete any user.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="userToDeleteGuid">The GUID of the user to delete.</param>
    /// <exception cref="ZootifyException">Thrown when the caller does not have permission.</exception>
    /// <exception cref="ZootifyException">Thrown when no user with the given GUID exists.</exception>
    [Transaction(ReadOnly = false)]
    public async Task DeleteUserAsync(
        IAuthentication authentication,
        Guid userToDeleteGuid)
    {
        var isSelf = authentication.UserGuid == userToDeleteGuid;

        // A regular user may delete no one but themselves (decided independently of the target).
        if (!isSelf && authentication.Role == UserRole.User)
        {
            throw new ZootifyException(ErrorCode.Forbidden);
        }

        var userToDelete = await _userRepository
            .GetUserByGuidWithAuditInformationAsync(userToDeleteGuid);

        if (userToDelete == null)
        {
            throw new ZootifyException(ErrorCode.NoUserFound);
        }

        // A moderator may only delete strictly-lower roles (i.e. regular users); an admin may delete
        // anyone. This role rule is evaluated before the active-state check so a moderator targeting a
        // peer/admin is consistently forbidden.
        if (!isSelf
            && authentication.Role == UserRole.Moderator
            && userToDelete.Role >= UserRole.Moderator)
        {
            throw new ZootifyException(ErrorCode.Forbidden);
        }

        // Only an active user can be deleted; an already-deleted (or otherwise inactive) account is
        // treated as not found.
        if (userToDelete.Status != UserStatus.Active)
        {
            throw new ZootifyException(ErrorCode.NoUserFound);
        }

        userToDelete.Status = UserStatus.Deleted;
        userToDelete.SetDeletedBy(authentication.UserGuid);

        await _userRepository.UpdateAsync(userToDelete);
    }
    
    /// <summary>
    /// Updates the authenticated user's own profile data after validating email format and detecting changes.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="updateOwnUserRequestDto">The new profile data to apply.</param>
    /// <exception cref="ZootifyException">Thrown when the requesting user is not active.</exception>
    /// <exception cref="ZootifyException">Thrown when no changes are detected.</exception>
    [Transaction(ReadOnly = false)]
    public async Task UpdateOwnUserAsync(
        IAuthentication authentication,
        UpdateOwnUserRequestDto updateOwnUserRequestDto)
    {
        // The object must be active to be updated; a non-active (blocked/deleted) caller is rejected.
        var requester =  await _userRepository
            .GetActiveUserByGuid(authentication.UserGuid);

        if (requester == null)
        {
            throw new ZootifyException(ErrorCode.ObjectIsNotActive);
        }

        HelperMethods.CheckEmail(updateOwnUserRequestDto.Email);

        var mappedUser = _mapper.Map<UserEntity>(updateOwnUserRequestDto);
        mappedUser.Role = requester.Role;

        mappedUser.FormatEmail();

        if (!requester.CheckChanges(mappedUser))
        {
            throw new ZootifyException(ErrorCode.NoUpdatedData);
        }

        requester.UpdateUserData(mappedUser, authentication.UserGuid);

        await _userRepository.UpdateAsync(requester);
    }

    /// <summary>
    /// Returns the authenticated user's own profile and settings data.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <returns>A response DTO containing the current user's data.</returns>
    /// <exception cref="ZootifyException">Thrown when no user with the given GUID exists.</exception>
    [Transaction(ReadOnly = true)]
    public async Task<GetOwnUserDataResponse> GetOwnUserDataAsync(IAuthentication authentication)
    {
        var user = await _userRepository
            .GetActiveUserByGuid(authentication.UserGuid);

        if (user == null)
        {
            throw new ZootifyException(ErrorCode.NoUserFound);
        }
        
        return _mapper.Map<GetOwnUserDataResponse>(user);
    }
    
}