using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.DTOs.Common.Response.Helpers;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Providers.Excel;
using Zootify_Backend.Model.Services.Common.Interfaces;

namespace Zootify_Backend.Model.Services.Common.Classes;

/// <summary>
/// Handles importing and exporting specimens via Excel workbooks, resolving the species by a fuzzy
/// match on the latin name and reusing the standard create pipeline for persistence.
/// </summary>
public class CommonSpecimenImportExportService : ICommonSpecimenImportExportService
{
    /// <summary>The minimum fuzzy similarity (0..1) required to accept a species name match.</summary>
    private const double SpeciesMatchThreshold = 0.7;

    private readonly ISpecimenExcelProvider _excelProvider;
    private readonly ITaxonomyRepository _taxonomyRepository;
    private readonly ITagRepository _tagRepository;
    private readonly ICommonCollectionService _commonCollectionService;
    private readonly ISpecimenRepository _specimenRepository;
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// Initializes a new <see cref="CommonSpecimenImportExportService"/> with its required dependencies.
    /// </summary>
    /// <param name="excelProvider">Provider for reading and writing Excel workbooks.</param>
    /// <param name="taxonomyRepository">Repository for taxonomy node lookups used by species matching.</param>
    /// <param name="tagRepository">Repository for tag lookups used to link existing tags.</param>
    /// <param name="commonCollectionService">Service used to resolve a collection's visible specimens for export.</param>
    /// <param name="specimenRepository">Repository for loading specimen entities for export.</param>
    /// <param name="scopeFactory">Factory used to create a fresh DI scope (and DbContext) per imported row.</param>
    public CommonSpecimenImportExportService(
        ISpecimenExcelProvider excelProvider,
        ITaxonomyRepository taxonomyRepository,
        ITagRepository tagRepository,
        ICommonCollectionService commonCollectionService,
        ISpecimenRepository specimenRepository,
        IServiceScopeFactory scopeFactory)
    {
        _excelProvider = excelProvider;
        _taxonomyRepository = taxonomyRepository;
        _tagRepository = tagRepository;
        _commonCollectionService = commonCollectionService;
        _specimenRepository = specimenRepository;
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Imports specimens from an Excel workbook. Each row's species is resolved by a fuzzy match on the
    /// latin name; tags are linked when they already exist and created otherwise; rows that cannot be
    /// parsed, matched, or created are skipped and reported.
    /// </summary>
    /// <param name="authentication">The authentication context of the importing user.</param>
    /// <param name="excelStream">The uploaded workbook stream.</param>
    /// <returns>The number of imported specimens and the list of failed rows with reasons.</returns>
    /// <exception cref="ZootifyException">Thrown when the stream is not a readable workbook.</exception>
    public async Task<ImportSpecimenResultDto> ImportAsync(IAuthentication authentication, Stream excelStream)
    {
        var parseResult = _excelProvider.ReadImportRows(excelStream);
        var failures = new List<ImportSpecimenFailureDto>(parseResult.Failures);
        var imported = 0;

        var allTaxonomyNodes = await _taxonomyRepository.GetAllNodesForTreeAsync();
        var speciesNodes = allTaxonomyNodes
            .Where(n => n.Rank == TaxonomyRank.Species)
            .ToList();

        var allTags = await _tagRepository.GetTagsByNameFilterAsync(null);
        var tagsByName = allTags
            .GroupBy(t => t.Tag, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Guid, StringComparer.OrdinalIgnoreCase);

        foreach (var row in parseResult.Rows)
        {
            var species = FuzzyMatch.BestMatch(
                row.TaxonomySpeciesLatinName,
                speciesNodes,
                n => n.Name,
                SpeciesMatchThreshold);

            if (species == null)
            {
                failures.Add(new ImportSpecimenFailureDto
                {
                    RowNumber = row.RowNumber,
                    Reason = $"No matching species found for \"{row.TaxonomySpeciesLatinName}\".",
                });
                continue;
            }

            try
            {
                await CreateSpecimenInIsolatedScopeAsync(authentication, row, species.Guid, species.Name, tagsByName);
                imported++;
            }
            catch (ZootifyException exception)
            {
                failures.Add(new ImportSpecimenFailureDto
                {
                    RowNumber = row.RowNumber,
                    Reason = exception.ErrorCode.ToString(),
                });
            }
        }

        return new ImportSpecimenResultDto
        {
            ImportedCount = imported,
            FailedCount = failures.Count,
            Failures = failures.OrderBy(f => f.RowNumber).ToList(),
        };
    }

    /// <summary>
    /// Exports the specimens of the given collection (respecting the caller's visibility rules) to an
    /// Excel workbook.
    /// </summary>
    /// <param name="authentication">The authentication context of the exporting user.</param>
    /// <param name="collectionGuid">The GUID of the collection whose specimens to export.</param>
    /// <returns>The workbook content as a byte array.</returns>
    /// <exception cref="ZootifyException">Thrown when the collection is not found.</exception>
    public async Task<byte[]> ExportCollectionAsync(IAuthentication authentication, Guid collectionGuid)
    {
        // Reuse the existing visibility + static/dynamic-collection dispatch, paging through all
        // visible specimens (page size is capped, so a single request can no longer fetch everything).
        var guids = new List<Guid>();
        var page = 1;
        while (true)
        {
            var paging = new BaseSortAndPagingDto { Page = page, PageSize = BaseSortAndPagingDto.MaxPageSize };
            var collection = await _commonCollectionService.GetAllSpecimensInCollectionAsync(
                authentication, collectionGuid, null, paging);

            guids.AddRange(collection.Specimens.Select(s => s.SpecimenGuid));

            if (collection.Specimens.Count == 0 || guids.Count >= collection.TotalCount)
            {
                break;
            }

            page++;
        }

        var entities = await _specimenRepository.GetSpecimensByGuidsWithSpeciesAsync(guids);

        var rows = entities.Select(ToExportRow).ToList();
        return _excelProvider.WriteExport(rows);
    }

    /// <summary>
    /// Creates a single specimen via the standard create pipeline in its own DI scope (and DbContext),
    /// so a failure in one row cannot affect the others.
    /// </summary>
    /// <param name="authentication">The authentication context of the importing user.</param>
    /// <param name="row">The parsed import row to create a specimen from.</param>
    /// <param name="speciesGuid">The GUID of the resolved taxonomy species.</param>
    /// <param name="speciesLatinName">The matched species' latin name, used as a fallback when the row has none.</param>
    /// <param name="tagsByName">Map of existing tag names to their GUIDs, used to link existing tags.</param>
    private async Task CreateSpecimenInIsolatedScopeAsync(
        IAuthentication authentication,
        ImportSpecimenRowDto row,
        Guid speciesGuid,
        string speciesLatinName,
        IReadOnlyDictionary<string, Guid> tagsByName)
    {
        var (existingTagGuids, tagsToCreate) = ResolveTags(row.Tags, tagsByName);

        var request = new CreateSpecimenRequestDto
        {
            Name = row.Name,
            // The Latin Name column is optional in the import template, but a specimen always requires
            // one, so fall back to the matched species' latin name when the row leaves it empty.
            LatinName = string.IsNullOrWhiteSpace(row.LatinName) ? speciesLatinName : row.LatinName!,
            Comment = row.Comment,
            Size = row.Size,
            Weight = row.Weight,
            Location = new GeoDto { Latitude = row.Latitude, Longitude = row.Longitude },
            DateOfDiscovery = row.DateOfDiscovery,
            TaxonomySpeciesGuid = speciesGuid,
            StorageBuilding = row.StorageBuilding,
            StorageCabinet = row.StorageCabinet,
            StorageShelf = row.StorageShelf,
            StorageCompartment = row.StorageCompartment,
            Tags = existingTagGuids,
            TagsToCreate = tagsToCreate,
        };

        await using var scope = _scopeFactory.CreateAsyncScope();
        var specimenService = scope.ServiceProvider.GetRequiredService<ICommonSpecimenService>();
        await specimenService.CreateSpecimenAsync(authentication, request);
    }

    /// <summary>
    /// Splits a comma-separated tag list into existing tag GUIDs (linked) and new tag names (created).
    /// </summary>
    /// <param name="tagsCsv">The comma-separated tag names from the import row, or <c>null</c>.</param>
    /// <param name="tagsByName">Map of existing tag names to their GUIDs.</param>
    /// <returns>A tuple of existing tag GUIDs to link and new tag names to create.</returns>
    private static (List<Guid> Existing, List<string> ToCreate) ResolveTags(
        string? tagsCsv,
        IReadOnlyDictionary<string, Guid> tagsByName)
    {
        var existing = new List<Guid>();
        var toCreate = new List<string>();

        if (string.IsNullOrWhiteSpace(tagsCsv))
        {
            return (existing, toCreate);
        }

        foreach (var tagName in tagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (tagsByName.TryGetValue(tagName, out var guid))
            {
                existing.Add(guid);
            }
            else
            {
                toCreate.Add(tagName);
            }
        }

        return (existing, toCreate);
    }

    /// <summary>
    /// Maps a specimen entity to an export row using the species node's name as the latin species name
    /// and a comma-separated list of its tag names.
    /// </summary>
    /// <param name="specimen">The specimen entity (with species and tags loaded) to map.</param>
    /// <returns>The export row representing the specimen.</returns>
    private static ImportSpecimenRowDto ToExportRow(SpecimenEntity specimen) => new()
    {
        RowNumber = 0,
        Name = specimen.Name,
        LatinName = specimen.LatinName,
        Latitude = specimen.Latitude,
        Longitude = specimen.Longitude,
        DateOfDiscovery = specimen.DateOfDiscovery,
        TaxonomySpeciesLatinName = specimen.Species?.Name ?? string.Empty,
        Comment = specimen.Comment,
        Size = specimen.Size,
        Weight = specimen.Weight,
        StorageBuilding = specimen.StorageBuilding,
        StorageCabinet = specimen.StorageCabinet,
        StorageShelf = specimen.StorageShelf,
        StorageCompartment = specimen.StorageCompartment,
        Tags = specimen.SpecimensTags.Count > 0
            ? string.Join(", ", specimen.SpecimensTags.Select(st => st.Tag.Tag))
            : null,
    };
}
