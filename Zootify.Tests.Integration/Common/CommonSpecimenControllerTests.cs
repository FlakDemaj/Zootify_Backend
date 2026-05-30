using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.Model.Providers.Nominatim;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Common;

public class CommonSpecimenControllerTests : IntegrationTestBase
{
    public CommonSpecimenControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }
    
    #region CreateSpecimen – Status & Approval

    [Fact]
    public async Task CreateSpecimen_AllExistingTaxonomies_SpecimenIsActiveNoApproval()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var genus = await SpecimenRepo.CreateTaxonomyNodeAsync("Panthera", TaxonomyRank.Genus);
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync(
            "leo", TaxonomyRank.Species, parentGuid: genus.Guid);

        SetupNominatimMock();
        AuthenticateAs(user);

        var request = BuildRequest([
            new TaxonomyDto { Guid = genus.Guid, Name = genus.Name, Rank = TaxonomyRank.Genus },
            new TaxonomyDto { Guid = species.Guid, Name = species.Name, Rank = TaxonomyRank.Species }
        ]);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CreatedObjectResponseDto>();
        result.Should().NotBeNull();

        var specimen = await GetSpecimenFromDbAsync(result!.CreatedObjectGuid);
        specimen.Should().NotBeNull();
        specimen!.Status.Should().Be(SpecimenStatus.Active);
        specimen.TaxonomySpeciesGuid.Should().Be(species.Guid);

        var approval = await GetApprovalForSpecimenFromDbAsync(result.CreatedObjectGuid);
        approval.Should().BeNull();
    }

    [Fact]
    public async Task CreateSpecimen_AllNewTaxonomies_SpecimenIsPendingWithApproval()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        SetupNominatimMock();
        AuthenticateAs(user);

        var request = BuildRequest([
            new TaxonomyDto { Name = "Panthera", Rank = TaxonomyRank.Genus },
            new TaxonomyDto { Name = "leo", Rank = TaxonomyRank.Species }
        ]);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CreatedObjectResponseDto>();
        result.Should().NotBeNull();

        var specimen = await GetSpecimenFromDbAsync(result!.CreatedObjectGuid);
        specimen.Should().NotBeNull();
        specimen!.Status.Should().Be(SpecimenStatus.Pending);

        var approval = await GetApprovalForSpecimenFromDbAsync(result.CreatedObjectGuid);
        approval.Should().NotBeNull();
        approval!.ApprovalStatus.Should().Be(ApprovalStatus.Pending);
    }

    [Fact]
    public async Task CreateSpecimen_MixedTaxonomies_SpecimenIsPendingWithApproval()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var genus = await SpecimenRepo.CreateTaxonomyNodeAsync("Panthera", TaxonomyRank.Genus);

        SetupNominatimMock();
        AuthenticateAs(user);

        var request = BuildRequest([
            new TaxonomyDto { Guid = genus.Guid, Name = genus.Name, Rank = TaxonomyRank.Genus },
            new TaxonomyDto { Name = "leo", Rank = TaxonomyRank.Species }
        ]);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CreatedObjectResponseDto>();
        result.Should().NotBeNull();

        var specimen = await GetSpecimenFromDbAsync(result!.CreatedObjectGuid);
        specimen!.Status.Should().Be(SpecimenStatus.Pending);

        var approval = await GetApprovalForSpecimenFromDbAsync(result.CreatedObjectGuid);
        approval.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateSpecimen_NewTaxonomies_TaxonomyNodesAreStoredAsPending()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        SetupNominatimMock();
        AuthenticateAs(user);

        var request = BuildRequest([
            new TaxonomyDto { Name = "Panthera", Rank = TaxonomyRank.Genus },
            new TaxonomyDto { Name = "leo", Rank = TaxonomyRank.Species }
        ]);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);
        var result = await response.Content.ReadFromJsonAsync<CreatedObjectResponseDto>();

        var specimen = await GetSpecimenFromDbAsync(result!.CreatedObjectGuid);
        var speciesNode = await GetTaxonomyNodeFromDbAsync(specimen!.TaxonomySpeciesGuid);

        speciesNode.Should().NotBeNull();
        speciesNode!.Name.Should().Be("leo");
        speciesNode.Rank.Should().Be(TaxonomyRank.Species);
        speciesNode.ApprovalStatus.Should().Be(ApprovalStatus.Pending);
    }

    #endregion

    #region CreateSpecimen – Tags

    [Fact]
    public async Task CreateSpecimen_WithExistingTags_TagsLinkedToSpecimen()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var tag1 = await SpecimenRepo.CreateTagAsync("mammal");
        var tag2 = await SpecimenRepo.CreateTagAsync("endangered");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);

        SetupNominatimMock();
        AuthenticateAs(user);

        var request = BuildRequest(
            taxonomies: [new TaxonomyDto { Guid = species.Guid, Name = species.Name, Rank = TaxonomyRank.Species }],
            tags: [tag1.Guid, tag2.Guid]);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CreatedObjectResponseDto>();
        var tagRelations = await GetTagRelationsForSpecimenFromDbAsync(result!.CreatedObjectGuid);

        tagRelations.Should().HaveCount(2);
        tagRelations.Select(r => r.TagGuid).Should().Contain(tag1.Guid);
        tagRelations.Select(r => r.TagGuid).Should().Contain(tag2.Guid);
    }

    [Fact]
    public async Task CreateSpecimen_WithNewTagsToCreate_TagsCreatedAndLinked()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);

        SetupNominatimMock();
        AuthenticateAs(user);

        var request = BuildRequest(
            taxonomies: [new TaxonomyDto { Guid = species.Guid, Name = species.Name, Rank = TaxonomyRank.Species }],
            tagsToCreate: ["mammal", "endangered"]);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CreatedObjectResponseDto>();
        var tagRelations = await GetTagRelationsForSpecimenFromDbAsync(result!.CreatedObjectGuid);

        tagRelations.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateSpecimen_WithExistingAndNewTags_AllTagsLinked()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var existingTag = await SpecimenRepo.CreateTagAsync("mammal");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);

        SetupNominatimMock();
        AuthenticateAs(user);

        var request = BuildRequest(
            taxonomies: [new TaxonomyDto { Guid = species.Guid, Name = species.Name, Rank = TaxonomyRank.Species }],
            tags: [existingTag.Guid],
            tagsToCreate: ["endangered"]);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CreatedObjectResponseDto>();
        var tagRelations = await GetTagRelationsForSpecimenFromDbAsync(result!.CreatedObjectGuid);

        tagRelations.Should().HaveCount(2);
        tagRelations.Select(r => r.TagGuid).Should().Contain(existingTag.Guid);
    }

    #endregion

    #region CreateSpecimen – Images

    [Fact]
    public async Task CreateSpecimen_WithBase64Images_ImagesStoredForSpecimen()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);

        SetupNominatimMock();
        AuthenticateAs(user);

        var request = BuildRequest(
            taxonomies: [new TaxonomyDto { Guid = species.Guid, Name = species.Name, Rank = TaxonomyRank.Species }],
            base64Images: ["base64image1", "base64image2", "base64image3"]);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CreatedObjectResponseDto>();
        var images = await GetImagesForSpecimenFromDbAsync(result!.CreatedObjectGuid);

        images.Should().HaveCount(3);
        images.Select(i => i.ImageUrl).Should().BeEquivalentTo(["base64image1", "base64image2", "base64image3"]);
    }

    #endregion

    #region CreateSpecimen – Geographic Location

    [Fact]
    public async Task CreateSpecimen_NominatimReturnsFullAddress_CreatesCompleteGeoChain()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);

        SetupNominatimMock(
            countryCode: "DE",
            state: "Bayern",
            city: "Munich",
            postcode: "80331",
            road: "Marienplatz");

        AuthenticateAs(user);

        var request = BuildRequest(
            [new TaxonomyDto { Guid = species.Guid, Name = species.Name, Rank = TaxonomyRank.Species }]);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var locations = await GetAllGeographicLocationsFromDbAsync();

        locations.Should().HaveCount(5);
        locations.Should().ContainSingle(l => l.Level == GeographicLevel.Country && l.Name == "DE");
        locations.Should().ContainSingle(l => l.Level == GeographicLevel.State && l.Name == "Bayern");
        locations.Should().ContainSingle(l => l.Level == GeographicLevel.City && l.Name == "Munich");
        locations.Should().ContainSingle(l => l.Level == GeographicLevel.Postcode && l.Name == "80331");
        locations.Should().ContainSingle(l => l.Level == GeographicLevel.Street && l.Name == "Marienplatz");
    }

    [Fact]
    public async Task CreateSpecimen_NominatimReturnsOnlyCountry_CreatesOnlyCountryGeoEntry()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);

        SetupNominatimMock(
            countryCode: "DE",
            state: null,
            city: null,
            postcode: null);

        AuthenticateAs(user);

        var request = BuildRequest(
            [new TaxonomyDto { Guid = species.Guid, Name = species.Name, Rank = TaxonomyRank.Species }]);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var locations = await GetAllGeographicLocationsFromDbAsync();

        locations.Should().HaveCount(1);
        locations.Single().Level.Should().Be(GeographicLevel.Country);
        locations.Single().Name.Should().Be("DE");
    }

    [Fact]
    public async Task CreateSpecimen_SpecimenGeoGuidPointsToMostSpecificLevel()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);

        SetupNominatimMock(
            countryCode: "DE",
            state: "Bayern",
            city: "Munich",
            postcode: null,
            road: null);

        AuthenticateAs(user);

        var request = BuildRequest(
            [new TaxonomyDto { Guid = species.Guid, Name = species.Name, Rank = TaxonomyRank.Species }]);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);
        var result = await response.Content.ReadFromJsonAsync<CreatedObjectResponseDto>();

        var specimen = await GetSpecimenFromDbAsync(result!.CreatedObjectGuid);
        var locations = await GetAllGeographicLocationsFromDbAsync();

        var cityEntry = locations.Single(l => l.Level == GeographicLevel.City);
        specimen!.GeographicLocationGuid.Should().Be(cityEntry.Guid);
    }

    [Fact]
    public async Task CreateSpecimen_SameLocationSubmittedTwice_ReusesExistingGeoEntities()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);

        SetupNominatimMock(countryCode: "DE", state: "Bayern", city: "Munich", postcode: null);

        AuthenticateAs(user);

        var request = BuildRequest(
            [new TaxonomyDto { Guid = species.Guid, Name = species.Name, Rank = TaxonomyRank.Species }]);

        await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);
        var locationsAfterFirst = await GetAllGeographicLocationsFromDbAsync();

        await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);
        var locationsAfterSecond = await GetAllGeographicLocationsFromDbAsync();

        locationsAfterSecond.Should().HaveCount(locationsAfterFirst.Count);
    }

    #endregion

    #region CreateSpecimen – Coordinates

    [Fact]
    public async Task CreateSpecimen_StoresLatitudeAndLongitudeOnSpecimen()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);

        SetupNominatimMock();
        AuthenticateAs(user);

        var request = BuildRequest(
            [new TaxonomyDto { Guid = species.Guid, Name = species.Name, Rank = TaxonomyRank.Species }]);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);
        var result = await response.Content.ReadFromJsonAsync<CreatedObjectResponseDto>();

        var specimen = await GetSpecimenFromDbAsync(result!.CreatedObjectGuid);
        specimen!.Latitude.Should().BeApproximately(request.Location.Latitude, 0.001);
        specimen.Longitude.Should().BeApproximately(request.Location.Longitude, 0.001);
    }

    #endregion

    #region CreateSpecimen – Error cases

    [Fact]
    public async Task CreateSpecimen_InvalidTagGuid_ReturnsNoTagWithThisGuid()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);

        SetupNominatimMock();
        AuthenticateAs(user);

        var request = BuildRequest(
            taxonomies: [new TaxonomyDto { Guid = species.Guid, Name = species.Name, Rank = TaxonomyRank.Species }],
            tags: [Guid.CreateVersion7()]);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoTagWithThisGuid);
    }

    [Fact]
    public async Task CreateSpecimen_InvalidTaxonomyGuid_ReturnsNoTaxonomyWithThisGuid()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");

        SetupNominatimMock();
        AuthenticateAs(user);

        var request = BuildRequest([
            new TaxonomyDto { Guid = Guid.CreateVersion7(), Name = "Unknown", Rank = TaxonomyRank.Species }
        ]);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoTaxonomyWithThisGuid);
    }

    [Fact]
    public async Task CreateSpecimen_NominatimReturnsNull_ReturnsAddressDoesNotExist()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);

        NominatimMock
            .Setup(x => x.SearchLocationAsync(It.IsAny<GeoDto>()))
            .ReturnsAsync((NominatimResponse?)null);

        AuthenticateAs(user);

        var request = BuildRequest(
            [new TaxonomyDto { Guid = species.Guid, Name = species.Name, Rank = TaxonomyRank.Species }]);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.AddressDoesNotExist);
    }

    [Fact]
    public async Task CreateSpecimen_NominatimReturnsNullCountryCode_ReturnsAddressDoesNotExist()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);

        NominatimMock
            .Setup(x => x.SearchLocationAsync(It.IsAny<GeoDto>()))
            .ReturnsAsync(new NominatimResponse
            {
                Address = new NominatimAddressResponse { CountryCode = null }
            });

        AuthenticateAs(user);

        var request = BuildRequest(
            [new TaxonomyDto { Guid = species.Guid, Name = species.Name, Rank = TaxonomyRank.Species }]);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.AddressDoesNotExist);
    }

    [Fact]
    public async Task CreateSpecimen_NotAuthenticated_ReturnsUnauthorized()
    {
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);

        var request = BuildRequest(
            [new TaxonomyDto { Guid = species.Guid, Name = species.Name, Rank = TaxonomyRank.Species }]);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
    
    #region Helpers

    private void SetupNominatimMock(
        string countryCode = "DE",
        string? state = "Bayern",
        string? city = "Munich",
        string? postcode = "80331",
        string? road = null,
        string? houseNumber = null)
    {
        NominatimMock
            .Setup(x => x.SearchLocationAsync(It.IsAny<GeoDto>()))
            .ReturnsAsync(new NominatimResponse
            {
                Address = new NominatimAddressResponse
                {
                    CountryCode = countryCode,
                    State = state,
                    City = city,
                    Postcode = postcode,
                    Road = road,
                    HouseNumber = houseNumber
                }
            });
    }

    private static CreateSpecimenRequestDto BuildRequest(
        List<TaxonomyDto> taxonomies,
        List<Guid>? tags = null,
        List<string>? tagsToCreate = null,
        List<string>? base64Images = null) => new()
    {
        Name = "Test Specimen",
        LatinName = "Testus specienus",
        DateOfDiscovery = DateTime.UtcNow,
        Location = new GeoDto { Latitude = 48.137, Longitude = 11.575 },
        Taxonomies = taxonomies,
        Tags = tags ?? [],
        TagsToCreate = tagsToCreate ?? [],
        Base64Images = base64Images ?? []
    };

    #endregion
}
