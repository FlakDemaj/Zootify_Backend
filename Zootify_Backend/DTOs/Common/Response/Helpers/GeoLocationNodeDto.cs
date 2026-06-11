using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Response.Helpers;

public class GeoLocationNodeDto
{
    public required string Name { get; set; }
    public GeographicLevel Level { get; set; }
}
