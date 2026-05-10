using System.Security.Cryptography;
using Zootify_Backend.Models.DataAccess.Entitys.Base;

namespace Zootify_Backend.Model.DataAccess.Entitys;

public class ResetPasswordTokenEntity : BaseGuidEntity
{
    public string Token { get; private set; } = string.Empty;

    public Guid UserGuid { get; private set; }

    public UserEntity? User { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// Creates a new cryptographically secure reset password token for the given user.
    /// </summary>
    /// <param name="userGuid">The GUID of the user to confirm.</param>
    /// <param name="validity">How long the token should remain valid.</param>
    public static ResetPasswordTokenEntity Create(Guid userGuid, TimeSpan validity)
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToHexString(tokenBytes);

        return new ResetPasswordTokenEntity
        {
            Token = token,
            UserGuid = userGuid,
            ExpiresAt = DateTime.UtcNow.Add(validity)
        };
    }

    /// <summary>
    /// Returns true if this token has passed its expiration date.
    /// </summary>
    public bool IsExpired()
    {
        return DateTime.UtcNow > ExpiresAt;
    }
}