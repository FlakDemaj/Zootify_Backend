using Zootify_Backend.Enums;

namespace Zootify_Backend;

public static class HelperMethods
{
    /// <summary>
    /// Validates that the email contains "@" and ".", and meets the minimum length requirement.
    /// Throws a <see cref="ZootifyException"/> with <see cref="ErrorCode.EmailError"/> on failure.
    /// </summary>
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