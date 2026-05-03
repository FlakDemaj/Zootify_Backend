using Zootify_Backend.Enums;

namespace Zootify_Backend.Models;

public class GeographicLocation
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public GeographicLevel Level { get; set; }

    public int? ParentId { get; set; }
    public GeographicLocation? Parent { get; set; }
    public ICollection<GeographicLocation> Children { get; set; } = new List<GeographicLocation>();

    public ICollection<Specimen> Specimens { get; set; } = new List<Specimen>();
}
