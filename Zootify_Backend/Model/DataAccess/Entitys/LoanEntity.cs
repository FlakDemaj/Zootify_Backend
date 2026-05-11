using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Models.DataAccess.Entitys.Base;

namespace Zootify_Backend.Models.DataAccess.Entitys;

public class LoanEntity : BaseGuidEntity
{
    public Guid SpecimenGuid { get; set; }
    
    public SpecimenEntity Specimen { get; set; } = null!;

    public Guid LenderGuid { get; set; }

    public UserEntity Lender { get; set; } = null!;

    public Guid? BorrowerGuid { get; set; }

    public UserEntity? Borrower { get; set; }

    public string? BorrowerExternalName { get; set; }

    public DateTime LoanDate { get; set; }
    
    public DateTime DueDate { get; set; }
    
    public DateTime? ReturnDate { get; set; }
    
    public string? Notes { get; set; }
}
