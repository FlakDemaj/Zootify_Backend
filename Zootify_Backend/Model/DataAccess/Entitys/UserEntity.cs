using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys.Base;
using Zootify_Backend.Models.DataAccess.Entitys;

namespace Zootify_Backend.Model.DataAccess.Entitys;

/// <summary>
/// Represents an application user with authentication state, login tracking, and token associations.
/// </summary>
public class UserEntity : BaseDeletionEntity
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PasswordHash { get; private set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.User;

    public UserStatus Status { get; set; } = UserStatus.Pending;

    public int LoginAttempts { get; private set; }

    public ICollection<SpecimenEntity> Specimens { get; set; } = new List<SpecimenEntity>();

    public ICollection<TaxonomyNodeEntity> ProposedTaxonomies { get; set; } = new List<TaxonomyNodeEntity>();

    public ICollection<LoanEntity> LoansAsLender { get; set; } = new List<LoanEntity>();

    public ICollection<LoanEntity> LoansAsBorrower { get; set; } = new List<LoanEntity>();

    public RegistrationTokenEntity? RegistrationToken { get; private set; }

    public ResetPasswordTokenEntity? ResetPasswordToken { get; private set; }

    /// <summary>
    /// Normalizes the email address to lowercase.
    /// </summary>
    public void FormatEmail()
    {
        Email = Email.ToLower();
    }

    /// <summary>
    /// Sets the user's status to <see cref="UserStatus.Active"/>.
    /// </summary>
    public void ActivateUser()
    {
        Status = UserStatus.Active;
    }

    /// <summary>
    /// Hashes the given plain-text password and stores the result.
    /// </summary>
    public void SetPassword(string password)
    {
        PasswordHash = PasswordHasher.Hash(password);
    }

    /// <summary>
    /// Creates a new registration token for this user and attaches it to the entity.
    /// </summary>
    /// <param name="expirationInHours">How many hours until the token expires.</param>
    /// <returns>The newly created <see cref="RegistrationTokenEntity"/>.</returns>
    public RegistrationTokenEntity CreateRegistrationToken(
        int expirationInHours)
    {
        var registrationToken = RegistrationTokenEntity.Create(
            Guid,
            TimeSpan.FromHours(expirationInHours));

        RegistrationToken = registrationToken;

        return registrationToken;
    }

    /// <summary>
    /// Creates a new reset-password token for this user and attaches it to the entity.
    /// </summary>
    /// <param name="expirationInHours">How many hours until the token expires.</param>
    /// <returns>The newly created <see cref="ResetPasswordTokenEntity"/>.</returns>
    public ResetPasswordTokenEntity CreateResetPasswordTokenEntityToken(
        int expirationInHours)
    {
        var resetPasswordToken = ResetPasswordTokenEntity.Create(
            Guid,
            TimeSpan.FromHours(expirationInHours));

        ResetPasswordToken = resetPasswordToken;

        return resetPasswordToken;
    }

    /// <summary>
    /// Detaches the registration token from this user (sets the navigation property to <c>null</c>).
    /// </summary>
    public void RemoveRegistrationToken()
    {
        RegistrationToken = null;
    }

    /// <summary>
    /// Verifies the given plain-text password against the stored hash.
    /// </summary>
    /// <returns><c>true</c> if the password matches; otherwise <c>false</c>.</returns>
    public bool CheckPassword(string password)
    {
        return PasswordHasher.VerifyPassword(password, PasswordHash!);
    }

    /// <summary>
    /// Increments the failed-login counter. Blocks the user once the counter reaches 5.
    /// </summary>
    public void IncrementLoginAttempts(
        int resetPasswordAttempts)
    {
        LoginAttempts++;

        if (LoginAttempts == resetPasswordAttempts)
        {
            Status = UserStatus.Blocked;
        }
    }

    /// <summary>
    /// Resets the failed-login counter to zero and restores the user's status to Active.
    /// </summary>
    public void ResetLoginAttempts()
    {
        LoginAttempts = 0;
    }

    /// <summary>
    /// Detaches the reset-password token from this user (sets the navigation property to <c>null</c>).
    /// </summary>
    public void RemoveResetPasswordToken()
    {
        ResetPasswordToken = null;
    }

    public bool CheckChanges(UserEntity newUserData)
    {
        return FirstName != newUserData.FirstName
               || LastName != newUserData.LastName
               || Email != newUserData.Email
               || Role != newUserData.Role;
    }

    public void UpdateUserData(
        UserEntity newUserData,
        Guid requesterGuid)
    {
        FirstName = newUserData.FirstName;
        LastName = newUserData.LastName;
        Role = newUserData.Role;
        Email = newUserData.Email;
        SetUpdatedBy(requesterGuid);
    }
}
