using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys.Base;
using Zootify_Backend.Models.DataAccess.Entitys;

namespace Zootify_Backend.Model.DataAccess.Entitys;

public class UserEntity : BaseDeletionEntity
{
    public string FirstName { get; private set; } = string.Empty;
    
    public string LastName { get; private set; } = string.Empty;
    
    public string Email { get; private set; } = string.Empty;
    
    public string? PasswordHash { get; private set; } = string.Empty;
    
    public UserRole Role { get; private set; } = UserRole.User;
    
    public UserStatus Status { get; private set; } = UserStatus.Pending;
    
    public int LoginAttempts { get; private set; }
    
    public ICollection<SpecimenEntity> Specimens { get; set; } = new List<SpecimenEntity>();
    
    public ICollection<TaxonomyNodeEntity> ProposedTaxonomies { get; set; } = new List<TaxonomyNodeEntity>();
    
    public ICollection<LoanEntity> LoansAsLender { get; set; } = new List<LoanEntity>();
    
    public ICollection<LoanEntity> LoansAsBorrower { get; set; } = new List<LoanEntity>();

    public RegistrationTokenEntity? RegistrationToken { get; private set; }

    public ResetPasswordTokenEntity? ResetPasswordToken { get; private set; }
    
    public void FormatEmail()
    {
        Email = Email.ToLower();
    }

    public void ActivateUser()
    {
        Status = UserStatus.Active;
    }

    public void SetPassword(string password)
    {
        PasswordHash = PasswordHasher.Hash(password);
    }
    
    public RegistrationTokenEntity CreateRegistrationToken(
        int expirationInHours)
    {
        var registrationToken = RegistrationTokenEntity.Create(
            Guid,
            TimeSpan.FromHours(expirationInHours));
        
        RegistrationToken = registrationToken;
        
        return registrationToken;
    }
    
    public ResetPasswordTokenEntity CreateResetPasswordTokenEntityToken(
        int expirationInHours)
    {
        var resetPasswordToken = ResetPasswordTokenEntity.Create(
            Guid,
            TimeSpan.FromHours(expirationInHours));
        
        ResetPasswordToken = resetPasswordToken;
        
        return resetPasswordToken;
    }
    
    public void RemoveRegistrationToken()
    {
        RegistrationToken = null;
    }
    
    public bool CheckPassword(string password)
    {
        return PasswordHasher.VerifyPassword(password, PasswordHash!);
    }
    
    public void IncrementLoginAttempts()
    {
        LoginAttempts++;

        if (LoginAttempts == 5)
        {
            Status = UserStatus.Blocked;
        }
    }
    
    public void ResetLoginAttempts()
    {
        LoginAttempts = 0;
        Status = UserStatus.Active;
    }
    
    public void RemoveResetPasswordToken()
    {
        ResetPasswordToken = null;
    }

}
