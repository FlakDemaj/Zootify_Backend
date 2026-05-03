namespace Zootify_Backend.Models;

public class SpecimenImage
{
    public int Id { get; set; }

    public int SpecimenId { get; set; }
    public Specimen Specimen { get; set; } = null!;

    public string ImageUrl { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
