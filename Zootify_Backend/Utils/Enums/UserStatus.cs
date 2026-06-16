namespace Zootify_Backend.Enums;

/// <summary>
/// The account state of a user.
/// </summary>
public enum UserStatus
{
    /// <summary>Registered but not yet activated (email confirmation pending).</summary>
    Pending = 0,

    /// <summary>Activated and able to sign in.</summary>
    Active = 1,

    /// <summary>Blocked by an administrator or after too many failed logins.</summary>
    Blocked = 2,

    /// <summary>Soft-deleted; retained until the purge job removes it permanently.</summary>
    Deleted = 3
}
