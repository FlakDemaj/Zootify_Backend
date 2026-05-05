using Zootify_Backend.Enums;
using Zootify_Backend.Models.DataAccess.Entitys.Base;

namespace Zootify_Backend.Models.DataAccess.Entitys;

public class UserEntity : BaseDeletionEntity
{
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public string PasswordHash { get; set; } = string.Empty;
    
    public UserRole Role { get; set; } = UserRole.User;
    
    public UserStatus Status { get; set; } = UserStatus.Pending;
    
    public ICollection<SpecimenEntity> Specimens { get; set; } = new List<SpecimenEntity>();
    
    public ICollection<TaxonomyNodeEntity> ProposedTaxonomies { get; set; } = new List<TaxonomyNodeEntity>();
    
    public ICollection<LoanEntity> LoansAsLender { get; set; } = new List<LoanEntity>();
    
    public ICollection<LoanEntity> LoansAsBorrower { get; set; } = new List<LoanEntity>();
}
