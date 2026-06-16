using AutoMapper;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.DTOs.Common.Response.Helpers;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.Mappers;

/// <summary>
/// AutoMapper profile defining the mappings between specimen DTOs and <see cref="SpecimenEntity"/>,
/// including the root-to-leaf geographic path projection for the detail view.
/// </summary>
public class SpecimenMapper : Profile
{
    /// <summary>
    /// Registers the create/update request, list, and detail mappings for specimens.
    /// </summary>
    public SpecimenMapper()
    {
        CreateMap<CreateSpecimenRequestDto, SpecimenEntity>(MemberList.None)
            .ForMember(d => d.Comment,
                opt =>
                    opt.MapFrom(src => src.Comment))
            .ForMember(d => d.Name,
                opt =>
                    opt.MapFrom(src => src.Name))
            .ForMember(d => d.LatinName,
                opt =>
                    opt.MapFrom(src => src.LatinName))
            .ForMember(d => d.DateOfDiscovery,
                opt =>
                    opt.MapFrom(src => src.DateOfDiscovery))
            .ForMember(d => d.TaxonomySpeciesGuid,
                opt =>
                    opt.MapFrom(src => src.TaxonomySpeciesGuid))
            .ForMember(d => d.StorageBuilding,
                opt =>
                    opt.MapFrom(src => src.StorageBuilding))
            .ForMember(d => d.StorageCabinet,
                opt =>
                    opt.MapFrom(src => src.StorageCabinet))
            .ForMember(d => d.StorageCompartment,
                opt => 
                    opt.MapFrom(src => src.StorageCompartment))
            .ForMember(d => d.StorageShelf,
                opt => 
                    opt.MapFrom(src => src.StorageShelf));

        CreateMap<string, TagEntity>(MemberList.None)
            .ForMember(d => d.Tag,
                opt =>
                    opt.MapFrom(src => src));

        CreateMap<SpecimenEntity, SpecimenDto>()
            .ForMember(d => d.SpecimenGuid,  o => o.MapFrom(s => s.Guid))
            .ForMember(d => d.Name,          o => o.MapFrom(s => s.Name))
            .ForMember(d => d.LatinName,     o => o.MapFrom(s => s.LatinName))
            .ForMember(d => d.Latitude,      o => o.MapFrom(s => s.Latitude))
            .ForMember(d => d.Longitude,     o => o.MapFrom(s => s.Longitude))
            .ForMember(d => d.IsPublic,      o => o.MapFrom(s => s.IsPublic))
            .ForMember(d => d.Status,        o => o.MapFrom(s => s.Status));

        CreateMap<SpecimenEntity, GetSpecimenDetailResponseDto>(MemberList.None)
            .ForMember(d => d.SpecimenGuid,
                o => o.MapFrom(s => s.Guid))
            .ForMember(d => d.Name,
                o => o.MapFrom(s => s.Name))
            .ForMember(d => d.LatinName,
                o => o.MapFrom(s => s.LatinName))
            .ForMember(d => d.StorageBuilding,
                o => o.MapFrom(s => s.StorageBuilding))
            .ForMember(d => d.StorageCabinet,
                o => o.MapFrom(s => s.StorageCabinet))
            .ForMember(d => d.StorageCompartment,
                o => o.MapFrom(s => s.StorageCompartment))
            .ForMember(d => d.StorageShelf,
                o => o.MapFrom(s => s.StorageShelf))
            .ForMember(d => d.CreatedAt,
                o => o.MapFrom(s => s.CreationDate))
            .ForMember(d => d.Comment,
                o => o.MapFrom(s => s.Comment))
            .ForMember(d => d.DateOfDiscovery,
                o => o.MapFrom(s => s.DateOfDiscovery))
            .ForMember(d => d.Longitude,
                o => o.MapFrom(s => s.Longitude))
            .ForMember(d => d.Latitude,
                o => o.MapFrom(s => s.Latitude))
            .ForMember(d => d.DeletedAt,
                o => o.MapFrom(s => s.DeletionDate))
            .ForMember(d => d.ImageGuid,
                o => o.MapFrom(s => s.Image != null ? s.Image.Guid : (Guid?)null))
            .ForMember(d => d.CreatedByName,
                o => o.MapFrom(s => s.CreatedByUser != null
                    ? $"{s.CreatedByUser.FirstName} {s.CreatedByUser.LastName}".Trim()
                    : null))
            .ForMember(d => d.UpdatedByName,
                o => o.MapFrom(s => s.UpdatedByUser != null
                    ? $"{s.UpdatedByUser.FirstName} {s.UpdatedByUser.LastName}".Trim()
                    : null))
            .ForMember(d => d.DeletedByName,
                o => o.MapFrom(s => s.DeletedByUser != null
                    ? $"{s.DeletedByUser.FirstName} {s.DeletedByUser.LastName}".Trim()
                    : null))
            .ForMember(d => d.GeoPath,
                o => o.MapFrom((s, _) =>
                {
                    // Walk the eagerly-loaded parent chain from the leaf (most specific level,
                    // e.g. house number) up to the root (country), then reverse so the resulting
                    // path runs root-to-leaf.
                    var path = new List<GeoLocationNodeDto>();
                    var current = s.GeographicLocation;
                    while (current != null)
                    {
                        path.Add(new GeoLocationNodeDto { Name = current.Name, Level = current.Level });
                        current = current.Parent;
                    }
                    path.Reverse();
                    return path;
                }))
            .ForMember(d => d.TaxonomyPath,
                o => o.Ignore());

        CreateMap<UpdateSpecimenRequestDto, SpecimenEntity>(MemberList.None)
            .ForMember(d => d.Comment,
                opt =>
                    opt.MapFrom(src => src.Comment))
            .ForMember(d => d.Name,
                opt =>
                    opt.MapFrom(src => src.Name))
            .ForMember(d => d.LatinName,
                opt =>
                    opt.MapFrom(src => src.LatinName))
            .ForMember(d => d.DateOfDiscovery,
                opt =>
                    opt.MapFrom(src => src.DateOfDiscovery))
            .ForMember(d => d.IsPublic,
                opt =>
                    opt.MapFrom(src => src.IsPublic));
    }
}