using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Model.Providers.Excel;
using Zootify_Backend.Model.Providers.Nominatim;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Common;

/// <summary>
/// Tests for Common Specimen Import Export Controller.
/// </summary>
public class CommonSpecimenImportExportControllerTests : IntegrationTestBase
{
    private const string ExcelContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static readonly SpecimenExcelProvider Excel = new();

    public CommonSpecimenImportExportControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    private void SetupNominatimMock()
    {
        NominatimMock
            .Setup(x => x.SearchLocationAsync(It.IsAny<Zootify_Backend.DTOs.Common.Response.Helpers.GeoDto>()))
            .ReturnsAsync(new NominatimResponse
            {
                Address = new NominatimAddressResponse
                {
                    CountryCode = "DE",
                    State = "Bayern",
                    City = "Munich",
                    Postcode = "80331",
                },
            });
    }

    private static MultipartFormDataContent BuildUpload(byte[] bytes, string fileName = "import.xlsx")
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(ExcelContentType);
        content.Add(fileContent, "file", fileName);
        return content;
    }

    private static ImportSpecimenRowDto Row(string name, string speciesLatinName) => new()
    {
        RowNumber = 0,
        Name = name,
        LatinName = speciesLatinName,
        Latitude = 50.0,
        Longitude = 8.0,
        DateOfDiscovery = new DateTime(2024, 5, 1),
        TaxonomySpeciesLatinName = speciesLatinName,
    };

    private async Task<List<SpecimenEntity>> GetSpecimensCreatedByAsync(Guid userGuid)
    {
        return await Fixture.DbContext.Set<SpecimenEntity>()
            .AsNoTracking()
            .Where(s => s.CreatedByUserGuid == userGuid)
            .ToListAsync();
    }

    #region Import

    /// <summary>
    /// Tests Import – Valid Rows – Creates Specimens Owned By User.
    /// </summary>
    [Fact]
    public async Task Import_ValidRows_CreatesSpecimensOwnedByUser()
    {
        SetupNominatimMock();
        var user = await UserRepo.CreateUserAsync("user@test.de");
        await SpecimenRepo.CreateTaxonomyNodeAsync("Papilio machaon", TaxonomyRank.Species);
        await SpecimenRepo.CreateTaxonomyNodeAsync("Carabus coriaceus", TaxonomyRank.Species);
        AuthenticateAs(user);

        var bytes = Excel.WriteExport(
        [
            Row("Schwalbenschwanz", "Papilio machaon"),
            Row("Lederlaufkäfer", "Carabus coriaceus"),
        ]);

        var response = await Client.PostAsync(
            $"{Routings.CommonSpecimenImportExportRouting}/import", BuildUpload(bytes));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ImportSpecimenResultDto>();
        result!.ImportedCount.Should().Be(2);
        result.Failures.Should().BeEmpty();

        var created = await GetSpecimensCreatedByAsync(user.Guid);
        created.Should().HaveCount(2);
        created.Should().OnlyContain(s => s.CreatedByUserGuid == user.Guid);
    }

    /// <summary>
    /// Tests Import – Size And Weight – Are Persisted On The Created Specimen.
    /// </summary>
    [Fact]
    public async Task Import_SizeAndWeight_ArePersisted()
    {
        SetupNominatimMock();
        var user = await UserRepo.CreateUserAsync("user@test.de");
        await SpecimenRepo.CreateTaxonomyNodeAsync("Papilio machaon", TaxonomyRank.Species);
        AuthenticateAs(user);

        var row = new ImportSpecimenRowDto
        {
            RowNumber = 0,
            Name = "Schwalbenschwanz",
            LatinName = "Papilio machaon",
            Latitude = 50.0,
            Longitude = 8.0,
            DateOfDiscovery = new DateTime(2024, 5, 1),
            TaxonomySpeciesLatinName = "Papilio machaon",
            Size = 42.5,
            Weight = 7.3,
        };
        var bytes = Excel.WriteExport([row]);

        var response = await Client.PostAsync(
            $"{Routings.CommonSpecimenImportExportRouting}/import", BuildUpload(bytes));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var created = await GetSpecimensCreatedByAsync(user.Guid);
        var specimen = created.Should().ContainSingle().Subject;
        specimen.Size.Should().Be(42.5);
        specimen.Weight.Should().Be(7.3);
    }

    /// <summary>
    /// Tests Import – Similar Species Name – Is Matched Fuzzily.
    /// </summary>
    [Fact]
    public async Task Import_SimilarSpeciesName_IsMatchedFuzzily()
    {
        SetupNominatimMock();
        var user = await UserRepo.CreateUserAsync("user@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("Papilio machaon", TaxonomyRank.Species);
        AuthenticateAs(user);

        // Slight typo in the latin name should still resolve to the existing species.
        var bytes = Excel.WriteExport([Row("Schwalbenschwanz", "Papilio machao")]);

        var response = await Client.PostAsync(
            $"{Routings.CommonSpecimenImportExportRouting}/import", BuildUpload(bytes));

        var result = await response.Content.ReadFromJsonAsync<ImportSpecimenResultDto>();
        result!.ImportedCount.Should().Be(1);

        var created = await GetSpecimensCreatedByAsync(user.Guid);
        created.Single().TaxonomySpeciesGuid.Should().Be(species.Guid);
    }

    /// <summary>
    /// Tests Import – No Matching Species – Row Is Reported And Others Imported.
    /// </summary>
    [Fact]
    public async Task Import_NoMatchingSpecies_RowIsReportedAndOthersImported()
    {
        SetupNominatimMock();
        var user = await UserRepo.CreateUserAsync("user@test.de");
        await SpecimenRepo.CreateTaxonomyNodeAsync("Papilio machaon", TaxonomyRank.Species);
        AuthenticateAs(user);

        var bytes = Excel.WriteExport(
        [
            Row("Valid", "Papilio machaon"),
            Row("Unknown", "Zzz Completely Different"),
        ]);

        var response = await Client.PostAsync(
            $"{Routings.CommonSpecimenImportExportRouting}/import", BuildUpload(bytes));

        var result = await response.Content.ReadFromJsonAsync<ImportSpecimenResultDto>();
        result!.ImportedCount.Should().Be(1);
        result.FailedCount.Should().Be(1);
        result.Failures.Should().ContainSingle();
    }

    /// <summary>
    /// Tests Import – Tags – Links Existing And Creates New.
    /// </summary>
    [Fact]
    public async Task Import_Tags_LinksExistingAndCreatesNew()
    {
        SetupNominatimMock();
        var user = await UserRepo.CreateUserAsync("user@test.de");
        await SpecimenRepo.CreateTaxonomyNodeAsync("Papilio machaon", TaxonomyRank.Species);
        var existingTag = await TagRepo.CreateTagAsync("Existing");
        AuthenticateAs(user);

        var bytes = Excel.WriteExport(
        [
            new ImportSpecimenRowDto
            {
                RowNumber = 0,
                Name = "Schwalbenschwanz",
                Latitude = 50.0,
                Longitude = 8.0,
                DateOfDiscovery = new DateTime(2024, 5, 1),
                TaxonomySpeciesLatinName = "Papilio machaon",
                Tags = "Existing, Brandnew",
            },
        ]);

        var response = await Client.PostAsync(
            $"{Routings.CommonSpecimenImportExportRouting}/import", BuildUpload(bytes));

        var result = await response.Content.ReadFromJsonAsync<ImportSpecimenResultDto>();
        result!.ImportedCount.Should().Be(1);

        var specimen = (await GetSpecimensCreatedByAsync(user.Guid)).Single();
        var relations = await Fixture.DbContext.Set<RelationSpecimensTagEntity>()
            .AsNoTracking()
            .Where(r => r.SpecimenGuid == specimen.Guid)
            .ToListAsync();
        var tags = await Fixture.DbContext.Set<TagEntity>().AsNoTracking().ToListAsync();

        relations.Should().HaveCount(2);
        relations.Select(r => r.TagGuid).Should().Contain(existingTag.Guid);
        tags.Should().Contain(t => t.Tag == "Brandnew");
        tags.Count(t => t.Tag == "Existing").Should().Be(1); // existing tag re-used, not duplicated
    }

    /// <summary>
    /// Tests Import – No File – Returns Invalid Import File.
    /// </summary>
    [Fact]
    public async Task Import_NoFile_ReturnsInvalidImportFile()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        AuthenticateAs(user);

        var response = await Client.PostAsync(
            $"{Routings.CommonSpecimenImportExportRouting}/import", new MultipartFormDataContent());

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.InvalidImportFile);
    }

    /// <summary>
    /// Tests Import – Unauthenticated – Returns Unauthorized.
    /// </summary>
    [Fact]
    public async Task Import_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await Client.PostAsync(
            $"{Routings.CommonSpecimenImportExportRouting}/import",
            BuildUpload(Excel.WriteExport([Row("X", "Papilio machaon")])));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Export

    /// <summary>
    /// Tests Export – Returns Collection Specimens As Excel.
    /// </summary>
    [Fact]
    public async Task Export_ReturnsCollectionSpecimensAsExcel()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("Papilio machaon", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var collection = await CollectionRepo.CreateCollectionAsync(isPublic: true, createdByUserGuid: user.Guid);

        var specimen = await SpecimenRepo.CreateSpecimenAsync(
            species.Guid, location.Guid, isPublic: true, createdByUserGuid: user.Guid, name: "Schwalbenschwanz");
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, specimen.Guid);

        AuthenticateAs(user);

        var response = await Client.GetAsync(
            $"{Routings.CommonSpecimenImportExportRouting}/export?collectionGuid={collection.Guid}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ExcelContentType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var stream = new MemoryStream(bytes);
        var parsed = Excel.ReadImportRows(stream);

        parsed.Rows.Should().ContainSingle();
        parsed.Rows.Single().Name.Should().Be("Schwalbenschwanz");
        parsed.Rows.Single().TaxonomySpeciesLatinName.Should().Be("Papilio machaon");
    }

    /// <summary>
    /// Tests Export – Non Public Specimen Of Other User – Is Excluded.
    /// </summary>
    [Fact]
    public async Task Export_NonPublicSpecimenOfOtherUser_IsExcluded()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var viewer = await UserRepo.CreateUserAsync("viewer@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("Papilio machaon", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var collection = await CollectionRepo.CreateCollectionAsync(isPublic: true, createdByUserGuid: owner.Guid);

        var publicSpecimen = await SpecimenRepo.CreateSpecimenAsync(
            species.Guid, location.Guid, isPublic: true, createdByUserGuid: owner.Guid, name: "Public");
        var privateSpecimen = await SpecimenRepo.CreateSpecimenAsync(
            species.Guid, location.Guid, isPublic: false, createdByUserGuid: owner.Guid, name: "Private");
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, publicSpecimen.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, privateSpecimen.Guid);

        AuthenticateAs(viewer);

        var response = await Client.GetAsync(
            $"{Routings.CommonSpecimenImportExportRouting}/export?collectionGuid={collection.Guid}");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var stream = new MemoryStream(bytes);
        var parsed = Excel.ReadImportRows(stream);

        parsed.Rows.Should().ContainSingle(r => r.Name == "Public");
        parsed.Rows.Should().NotContain(r => r.Name == "Private");
    }

    #endregion
}
