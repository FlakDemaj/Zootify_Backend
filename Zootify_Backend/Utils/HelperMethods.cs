using Zootify_Backend.Enums;

namespace Zootify_Backend;

public static class HelperMethods
{
    /// <summary>
    /// Validates that the email contains "@" and ".", and meets the minimum length requirement.
    /// Throws a <see cref="ZootifyException"/> with <see cref="ErrorCode.EmailError"/> on failure.
    /// </summary>
    /// <param name="email">The email address to validate.</param>
    /// <exception cref="ZootifyException">Thrown with <see cref="ErrorCode.EmailError"/> when the email is invalid.</exception>
    public static void CheckEmail(string email)
    {
        if (!email.Contains("@")
            || !email.Contains(".")
            || email.Length < 7)
        {
            throw new ZootifyException(ErrorCode.EmailError);
        }
    }
}