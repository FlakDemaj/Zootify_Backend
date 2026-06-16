namespace Zootify_Backend.Enums;

/// <summary>
/// Role name constants (matching the <see cref="UserRole"/> enum names) for use in <c>[Authorize]</c> attributes.
/// </summary>
public static class Roles
{
    /// <summary>The "User" role name.</summary>
    public const string User = nameof(UserRole.User);

    /// <summary>The "Admin" role name.</summary>
    public const string Admin = nameof(UserRole.Admin);

    /// <summary>The "Moderator" role name.</summary>
    public const string Moderator = nameof(UserRole.Moderator);
}