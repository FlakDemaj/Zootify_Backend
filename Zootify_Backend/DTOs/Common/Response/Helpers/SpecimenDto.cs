using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Response.Helpers;

/// <summary>
/// A lightweight specimen entry used within collection listings.
/// </summary>
public class SpecimenDto
{
    /// <summary>The specimen's GUID.</summary>
    public Guid SpecimenGuid { get; init; }

    /// <summary>The specimen's common display name.</summary>
    public required string Name { get; init; }

    /// <summary>The specimen's latin name.</summary>
    public required string LatinName  { get; init; }

    /// <summary>The latitude of the discovery location.</summary>
    public required double Latitude { get; init; }

    /// <summary>The longitude of the discovery location.</summary>
    public required double Longitude { get; init; }

    /// <summary>Whether the specimen is publicly visible.</summary>
    public bool IsPublic { get; init; } = true;

    /// <summary>The specimen's lifecycle status.</summary>
    public SpecimenStatus Status { get; init; } =  SpecimenStatus.Active;
}
