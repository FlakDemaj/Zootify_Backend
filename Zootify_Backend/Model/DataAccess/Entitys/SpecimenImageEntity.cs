using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify_Backend.Model.DataAccess.Entitys;

/// <summary>
/// Stores the Base64-encoded image attached to a specimen along with its upload timestamp.
/// </summary>
public class SpecimenImageEntity : BaseGuidEntity
{
    public Guid SpecimenGuid { get; set; }

    public SpecimenEntity Specimen { get; set; } = null!;

    public string ImageBase64 { get; set; } = string.Empty;
    
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
