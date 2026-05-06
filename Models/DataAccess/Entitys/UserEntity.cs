using Zootify_Backend.Enums;
using Zootify_Backend.Models.Auth;
using Zootify_Backend.Models.DataAccess.Entitys.Base;

namespace Zootify_Backend.Models.DataAccess.Entitys;

public class UserEntity : BaseDeletionEntity
{
    public string FirstName { get; private set; } = string.Empty;
    
    public string LastName { get; private set; } = string.Empty;
    
    public string Email { get; private set; } = string.Empty;
    
    public string? PasswordHash { get; private set; } = string.Empty;
    
    public UserRole Role { get; private set; } = UserRole.User;
    
    public UserStatus Status { get; private set; } = UserStatus.Pending;
    
    public ICollection<SpecimenEntity> Specimens { get; set; } = new List<SpecimenEntity>();
    
    public ICollection<TaxonomyNodeEntity> ProposedTaxonomies { get; set; } = new List<TaxonomyNodeEntity>();
    
    public ICollection<LoanEntity> LoansAsLender { get; set; } = new List<LoanEntity>();
    
    public ICollection<LoanEntity> LoansAsBorrower { get; set; } = new List<LoanEntity>();

    public RegistrationTokenEntity? RegistrationToken { get; private set; }
    
    public void CheckEmail()
    {
        if (!Email.Contains("@")
            || !Email.Contains(".")
            || Email.Length < 7)
        {
            throw new ZootifyException(ErrorCode.EmailError);
        }
    }

    public void ConfirmUser()
    {
        Status = UserStatus.Active;
    }

    public void HashPassword(string password)
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
    
    public void RemoveRegistrationToken(RegistrationTokenEntity registrationToken)
    {
        RegistrationToken = null;
    }
}
