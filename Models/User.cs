using Zootify_Backend.Enums;

namespace Zootify_Backend.Models;

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public UserStatus Status { get; set; } = UserStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Specimen> Specimens { get; set; } = new List<Specimen>();
    public ICollection<TaxonomyNode> ProposedTaxonomies { get; set; } = new List<TaxonomyNode>();
    public ICollection<Loan> LoansAsLender { get; set; } = new List<Loan>();
    public ICollection<Loan> LoansAsBorrower { get; set; } = new List<Loan>();
}
