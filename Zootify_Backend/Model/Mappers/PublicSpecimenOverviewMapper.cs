using AutoMapper;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.DTOs.Common.Response.Helpers;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.Mappers;

/// <summary>
/// AutoMapper profile that maps a <see cref="SpecimenEntity"/> to the public overview DTO,
/// flattening the geographic location chain into a path and resolving the taxonomic order.
/// </summary>
public class PublicSpecimenOverviewMapper : Profile
{
    /// <summary>
    /// Configures the mapping from <see cref="SpecimenEntity"/> to <see cref="PublicSpecimenOverviewDto"/>.
    /// </summary>
    public PublicSpecimenOverviewMapper()
    {
        CreateMap<SpecimenEntity, PublicSpecimenOverviewDto>()
            .ForMember(d => d.Name,
                o => o.MapFrom(s => s.Name))
            .ForMember(d => d.LatinName,
                o => o.MapFrom(s => s.LatinName))
            .ForMember(d => d.Latitude,
                o => o.MapFrom(s => s.Latitude))
            .ForMember(d => d.Longitude,
                o => o.MapFrom(s => s.Longitude))
            .ForMember(d => d.CreatedAt,
                o => o.MapFrom(s => s.CreationDate))
            .ForMember(d => d.ImageGuid,
                o => o.MapFrom(s => s.Image != null ? (Guid?)s.Image.Guid : null))
            .ForMember(d => d.LocationPath,
                o => o.MapFrom((s, _) => BuildLocationPath(s.GeographicLocation)))
            .ForMember(d => d.City,
                o => o.MapFrom((s, _) => ResolveCity(s.GeographicLocation)))
            .ForMember(d => d.TaxonGroup,
                o => o.MapFrom((s, _) => ResolveTaxonGroup(s.Species)));
    }

    /// <summary>
    /// Walks the geographic location parent chain from the leaf up to the root and returns the
    /// resulting nodes ordered root-to-leaf (e.g. Country → State → City → House number).
    /// </summary>
    /// <param name="leaf">The most specific geographic location of the specimen.</param>
    /// <returns>The ordered location path; empty if no location is set.</returns>
    private static List<GeoLocationNodeDto> BuildLocationPath(GeographicLocationEntity? leaf)
    {
        var path = new List<GeoLocationNodeDto>();
        var current = leaf;
        while (current != null)
        {
            path.Add(new GeoLocationNodeDto { Name = current.Name, Level = current.Level });
            current = current.Parent;
        }

        path.Reverse();
        return path;
    }

    /// <summary>
    /// Returns the name of the city-level node within the location chain, or <c>null</c> if absent.
    /// </summary>
    /// <param name="leaf">The most specific geographic location of the specimen.</param>
    /// <returns>The city name, or <c>null</c> when the chain has no city-level node.</returns>
    private static string? ResolveCity(GeographicLocationEntity? leaf)
    {
        var current = leaf;
        while (current != null)
        {
            if (current.Level == GeographicLevel.City)
            {
                return current.Name;
            }

            current = current.Parent;
        }

        return null;
    }

    /// <summary>
    /// Walks the species' taxonomy parent chain to the node of rank <see cref="TaxonomyRank.Order"/>
    /// and returns its name (e.g. "Lepidoptera"), or an empty string if none is found.
    /// </summary>
    /// <param name="species">The species-rank taxonomy node of the specimen.</param>
    /// <returns>The taxonomic order name, or an empty string when no order ancestor exists.</returns>
    private static string ResolveTaxonGroup(TaxonomyNodeEntity? species)
    {
        var current = species;
        while (current != null)
        {
            if (current.Rank == TaxonomyRank.Order)
            {
                return current.Name;
            }

            current = current.Parent;
        }

        return string.Empty;
    }
}
