namespace Zootify_Backend.Enums;

/// <summary>
/// The authorization role of a user, ordered by ascending privilege.
/// </summary>
public enum UserRole
{
    /// <summary>Standard user with access to their own data only.</summary>
    User = 0,

    /// <summary>Moderator who can review and approve content.</summary>
    Moderator = 1,

    /// <summary>Administrator with full management privileges.</summary>
    Admin = 2
}
