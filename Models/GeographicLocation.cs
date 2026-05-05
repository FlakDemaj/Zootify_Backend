using Zootify_Backend.Enums;

namespace Zootify_Backend.Models;

// Repräsentiert einen geografischen Ort (Land, Bundesland, Kreis oder Stadt)
public class GeographicLocation
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public GeographicLevel Level { get; set; } // z.B. Country, State, City

    // Elternelement für die Hierarchie (z.B. Siegen → NRW → Deutschland)
    public int? ParentId { get; set; }
    public GeographicLocation? Parent { get; set; }
    public ICollection<GeographicLocation> Children { get; set; } = new List<GeographicLocation>();

    // Alle Funde die an diesem Ort gemacht wurden
    public ICollection<Specimen> Specimens { get; set; } = new List<Specimen>();
}
