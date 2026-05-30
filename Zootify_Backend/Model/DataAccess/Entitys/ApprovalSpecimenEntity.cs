using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify_Backend.Model.DataAccess.Entitys;

public class ApprovalSpecimenEntity : BaseGuidEntity
{ 
    public Guid SpecimenGuid { get; set; }
    
    public ApprovalStatus ApprovalStatus { get; set; }
       
    public Guid? ApprovedByUserGuid { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public bool IsTaken { get; set; }
   
    public string Comment { get; set; }
    
    public UserEntity? ApprovedByUser { get; set; }
}