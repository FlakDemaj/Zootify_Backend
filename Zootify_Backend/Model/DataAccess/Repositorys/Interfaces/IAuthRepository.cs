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
    public Task<UserEntity?> GetUserByEmailAsync(string email, bool tracking = false);

    /// <summary>
    /// Returns the user whose reset-password token matches the given token string,
    /// eagerly loading the token.
    /// </summary>
    public Task<UserEntity?> GetUserByResetPasswordTokenWithResetPassword(string token);

    /// <summary>
    /// Returns the user with the given email, eagerly loading their reset-password token.
    /// </summary>
    public Task<UserEntity?> GetUserByEmailWithResetPassword(string email);

    /// <summary>
    /// Returns the user whose registration token matches the given token string,
    /// eagerly loading the token.
    /// </summary>
    public Task<UserEntity?> GetUserByRegistrationTokenAsync(string token);

    /// <summary>
    /// Returns the user with the given email, eagerly loading their registration token.
    /// </summary>
    public Task<UserEntity?> GetUserByEmailWithRegistrationToken(string email);
}
