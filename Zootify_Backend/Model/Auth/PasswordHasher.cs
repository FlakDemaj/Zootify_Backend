namespace Zootify_Backend.Model.Auth;

/// <summary>
/// Provides BCrypt-based password hashing and verification.
/// </summary>
public static class PasswordHasher
{
    /// <summary>
    /// Hashes a plain-text password using BCrypt.
    /// </summary>
    /// <param name="password">The plain-text password to hash.</param>
    /// <returns>The BCrypt hash of the password.</returns>
    public static string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    /// <summary>
    /// Verifies a plain-text password against a stored BCrypt hash.
    /// </summary>
    /// <param name="password">The plain-text password to verify.</param>
    /// <param name="hashedPassword">The stored BCrypt hash to compare against.</param>
    /// <returns><c>true</c> if the password matches the hash; otherwise <c>false</c>.</returns>
    public static bool VerifyPassword(string password, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }
}
