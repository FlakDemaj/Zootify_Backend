using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Extensions;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Models.DataAccess.DBContext;

namespace Zootify_Backend.Model.DataAccess.Repositorys.System.Classes;

/// <summary>
/// Repository for querying and persisting <see cref="UserEntity"/> records.
/// </summary>
public class AuthRepository : BaseRepository<UserEntity>, IAuthRepository
{
    /// <summary>
    /// Initializes a new <see cref="AuthRepository"/> with the given database context.
    /// </summary>
    public AuthRepository(ZootifyDbContext context)
        : base(context)
    {
    }

    /// <summary>
    /// Returns the user with the given email address, or <c>null</c> if not found.
    /// </summary>
    /// <param name="email">The email address to search for.</param>
    /// <param name="tracking">Whether to enable EF change tracking on the result.</param>
    public async Task<UserEntity?> GetUserByEmailAsync(
        string email, bool tracking = false)
    {
        return await
            Context
                .GetTrackingSet<UserEntity>(tracking)
                .Where(x => x.Email == email)
                .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Returns the user with the given email, eagerly loading their reset-password token.
    /// Always uses change tracking so the token can be modified.
    /// </summary>
    public async Task<UserEntity?> GetUserByEmailWithResetPassword(
        string email)
    {
        return await
            Context
                .GetTrackingSet<UserEntity>(true)
                .Include(u => u.ResetPasswordToken)
                .Where(x => x.Email == email)
                .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Returns the user whose reset-password token matches the given token string,
    /// eagerly loading the token. Always uses change tracking.
    /// </summary>
    public async Task<UserEntity?> GetUserByResetPasswordTokenWithResetPassword(
        string token)
    {
        return await
            Context
                .GetTrackingSet<UserEntity>(true)
                .Include(u => u.ResetPasswordToken)
                .Where(u => u.ResetPasswordToken != null
                            && u.ResetPasswordToken.Token == token)
                .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Returns the user with the given email, eagerly loading their registration token.
    /// Always uses change tracking so the token can be replaced.
    /// </summary>
    public async Task<UserEntity?> GetUserByEmailWithRegistrationToken(string email)
    {
        return await
            Context
                .GetTrackingSet<UserEntity>(true)
                .Include(u => u.RegistrationToken)
                .Where(x => x.Email == email)
                .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Returns the user whose registration token matches the given token string,
    /// eagerly loading the token. Always uses change tracking.
    /// </summary>
    public async Task<UserEntity?> GetUserByRegistrationTokenAsync(string token)
    {
        return await
            Context
                .GetTrackingSet<UserEntity>(true)
                .Include(u => u.RegistrationToken)
                .Where(u => u.RegistrationToken != null
                            && u.RegistrationToken.Token == token)
                .FirstOrDefaultAsync();
    }
}
