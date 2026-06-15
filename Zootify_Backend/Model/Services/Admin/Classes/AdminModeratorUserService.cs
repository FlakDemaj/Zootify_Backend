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

/// <summary>
/// Handles user management business logic for Admin and Moderator roles.
/// </summary>
public class AdminModeratorUserService : IAdminModeratorUserService
{
    private readonly IUserRepository _userRepository;

    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="AdminModeratorUserService"/> with its required dependencies.
    /// </summary>
    /// <param name="userRepository">Repository for user persistence operations.</param>
    /// <param name="mapper">AutoMapper instance for object mapping.</param>
    public AdminModeratorUserService(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    /// <summary>
    /// Returns detailed information for a single user identified by their GUID.
    /// </summary>
    /// <param name="guid">The GUID of the user to retrieve.</param>
    /// <returns>A detailed user response DTO.</returns>
    /// <exception cref="ZootifyException">Thrown when no user with the given GUID exists.</exception>
    [Transaction(ReadOnly = true)]
    public async Task<GetUserResponseDto> GetUserByGuidAsync(Guid guid)
    {
        var user = await _userRepository.GetUserByGuidWithAuditInformationAsync(guid);

        if (user == null)
        {
            throw new ZootifyException(ErrorCode.NoUserFound);
        }

        return _mapper.Map<GetUserResponseDto>(user);
    }

    /// <summary>
    /// Returns a filtered and paged list of all users in the system.
    /// </summary>
    /// <param name="query">Filter, sorting, and paging parameters.</param>
    /// <returns>A list of user response DTOs matching the filter criteria.</returns>
    [Transaction(ReadOnly = true)]
    public async Task<List<GetAllUserResponseDto>> GetAllUsersAsync(GetAllUsersFilterDto query)
    {
        var users = await _userRepository.GetAllUsersAsync(query);
        return _mapper.Map<List<GetAllUserResponseDto>>(users);
    }

    /// <summary>
    /// Updates the data of an existing user. Only Admins may perform this action and cannot update themselves.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting admin.</param>
    /// <param name="userToUpdateGuid">The GUID of the user to update.</param>
    /// <param name="updateUserRequestDto">The new data to apply to the user.</param>
    /// <exception cref="ZootifyException">Thrown when the caller does not have permission.</exception>
    /// <exception cref="ZootifyException">Thrown when no user with the given GUID exists.</exception>
    /// <exception cref="ZootifyException">Thrown when no changes are detected.</exception>
    [Transaction(ReadOnly = false)]
    public async Task UpdateUserAsync(
        IAuthentication authentication,
        Guid userToUpdateGuid,
        UpdateUserRequestDto updateUserRequestDto)
    {
        if (authentication.Role == UserRole.User ||
            authentication.UserGuid == userToUpdateGuid)
        {
            throw new ZootifyException(ErrorCode.Forbidden);
        }

        var requester = await _userRepository.GetActiveUserByGuid(authentication.UserGuid);

        if (requester == null)
        {
            throw new ZootifyException(ErrorCode.NoUserFound);
        }

        var oldUser = await _userRepository.GetActiveOrBlockedUserByGuid(userToUpdateGuid);

        if (oldUser == null)
        {
            throw new ZootifyException(ErrorCode.NoUserFound);
        }
        
        if (authentication.Role <= oldUser.Role
            && authentication.Role == UserRole.Moderator)
        {
            throw new ZootifyException(ErrorCode.Forbidden);
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

    /// <summary>
    /// Re-activates a previously deleted user account.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting admin.</param>
    /// <param name="userToActivateGuid">The GUID of the deleted user to activate.</param>
    /// <exception cref="ZootifyException">Thrown when no user with the given GUID exists.</exception>
    [Transaction(ReadOnly = false)]
    public async Task ActivateUserAsync(
        IAuthentication authentication,
        Guid userToActivateGuid)
    {
        var userToActivate = await _userRepository.GetDeletedUserByGuid(userToActivateGuid);

        if (userToActivate == null)
        {
            throw new ZootifyException(ErrorCode.NoUserFound);
        }

        userToActivate.Status = UserStatus.Active;
        userToActivate.RemoveDeletion();
        userToActivate.SetUpdatedBy(authentication.UserGuid);

        await _userRepository.UpdateAsync(userToActivate);
    }
}
