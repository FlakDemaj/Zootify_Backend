using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify_Backend.Model.DataAccess.Entitys;

public class SpecimenImageEntity : BaseGuidEntity
{
    public Guid SpecimenGuid { get; set; }

    public SpecimenEntity Specimen { get; set; } = null!;

    public string ImageBase64 { get; set; } = string.Empty;
    
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
