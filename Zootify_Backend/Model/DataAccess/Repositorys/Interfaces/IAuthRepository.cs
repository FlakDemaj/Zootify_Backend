using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

/// <summary>
/// Extends <see cref="IBaseRepository{T}"/> with user-specific query operations.
/// </summary>
public interface IAuthRepository : IBaseRepository<UserEntity>
{
    /// <summary>
    /// Returns the user with the given email address, or <c>null</c> if not found.
    /// </summary>
    /// <param name="email">The email address to search for.</param>
    /// <param name="tracking">Whether to enable EF change tracking on the result.</param>
    public Task<UserEntity?> GetUserByEmailWithSettingsAsync(string email, bool tracking = false);

    /// <summary>
    /// Returns the user whose reset-password token matches the given token string,
    /// eagerly loading the token.
    /// </summary>
    /// <param name="token">The reset-password token string to search for.</param>
    public Task<UserEntity?> GetUserByResetPasswordTokenWithResetPassword(string token);

    /// <summary>
    /// Returns the user with the given email, eagerly loading their reset-password token.
    /// </summary>
    /// <param name="email">The email address to search for.</param>
    public Task<UserEntity?> GetUserByEmailWithResetPassword(string email);

    /// <summary>
    /// Returns the user whose registration token matches the given token string,
    /// eagerly loading the token.
    /// </summary>
    /// <param name="token">The registration token string to search for.</param>
    public Task<UserEntity?> GetUserByRegistrationTokenAsync(string token);

    /// <summary>
    /// Returns the user with the given email, eagerly loading their registration token.
    /// </summary>
    /// <param name="email">The email address to search for.</param>
    public Task<UserEntity?> GetUserByEmailWithRegistrationToken(string email);
}
