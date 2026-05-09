using Zootify_Backend.Models.DataAccess.Entitys.Base;

namespace Zootify_Backend.Models.DataAccess.Entitys;

public class SpecimenImageEntity : BaseGuidEntity
{
    public Guid SpecimenGuid { get; set; }

    public SpecimenEntity Specimen { get; set; } = null!;

    public string ImageUrl { get; set; } = string.Empty;
    
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
