using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.DTOs.Common.Response.Helpers;
using Zootify_Backend.Model.Providers.Nominatim;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Common;

/// <summary>
/// Tests for Common Specimen Controller.
/// </summary>
public class CommonSpecimenControllerTests : IntegrationTestBase
{
    public CommonSpecimenControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    #region CreateSpecimen

    /// <summary>
    /// Tests Create Specimen – Valid Species Guid – Returns Created Specimen.
    /// </summary>
    [Fact]
    public async Task CreateSpecimen_ValidSpeciesGuid_ReturnsCreatedSpecimen()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);

        SetupNominatimMock();
        AuthenticateAs(user);

        var request = BuildRequest(species.Guid);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CreatedObjectResponseDto>();
        result.Should().NotBeNull();

        var specimen = await GetSpecimenFromDbAsync(result!.CreatedObjectGuid);
        specimen.Should().NotBeNull();
        specimen!.TaxonomySpeciesGuid.Should().Be(species.Guid);
        specimen.CreatedByUserGuid.Should().Be(user.Guid);
    }

    /// <summary>
    /// Tests Create Specimen – All Fields Persisted.
    /// </summary>
    [Fact]
    public async Task CreateSpecimen_AllFieldsPersisted()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var dateOfDiscovery = new DateTime(2020, 6, 15, 0, 0, 0, DateTimeKind.Utc);

        SetupNominatimMock();
        AuthenticateAs(user);

        var request = BuildRequest(
            species.Guid,
            name:               "Lion",
            latinName:          "Panthera leo",
            dateOfDiscovery:    dateOfDiscovery,
            comment:            "Found near the river.",
            storageBuilding:    "Building A",
            storageCabinet:     "Cabinet 3",
            storageShelf:       "Shelf 2",
            storageCompartment: "Compartment B");

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CreatedObjectResponseDto>();
        var specimen = await GetSpecimenFromDbAsync(result!.CreatedObjectGuid);

        specimen!.Name.Should().Be("Lion");
        specimen.LatinName.Should().Be("Panthera leo");
        specimen.DateOfDiscovery.Should().BeCloseTo(dateOfDiscovery, TimeSpan.FromSeconds(1));
        specimen.Comment.Should().Be("Found near the river.");
        specimen.StorageBuilding.Should().Be("Building A");
        specimen.StorageCabinet.Should().Be("Cabinet 3");
        specimen.StorageShelf.Should().Be("Shelf 2");
        specimen.StorageCompartment.Should().Be("Compartment B");
        specimen.TaxonomySpeciesGuid.Should().Be(species.Guid);
        specimen.CreatedByUserGuid.Should().Be(user.Guid);
    }

    #endregion

    #region CreateSpecimen – Tags

    /// <summary>
    /// Tests Create Specimen – With Existing Tags – Tags Linked To Specimen.
    /// </summary>
    [Fact]
    public async Task CreateSpecimen_WithExistingTags_TagsLinkedToSpecimen()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var tag1 = await TagRepo.CreateTagAsync("mammal");
        var tag2 = await TagRepo.CreateTagAsync("endangered");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);

        SetupNominatimMock();
        AuthenticateAs(user);

        var request = BuildRequest(species.Guid, tags: [tag1.Guid, tag2.Guid]);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CreatedObjectResponseDto>();
        var tagRelations = await GetTagRelationsForSpecimenFromDbAsync(result!.CreatedObjectGuid);

        tagRelations.Should().HaveCount(2);
        tagRelations.Select(r => r.TagGuid).Should().Contain(tag1.Guid);
        tagRelations.Select(r => r.TagGuid).Should().Contain(tag2.Guid);
    }

    /// <summary>
    /// Tests Create Specimen – With New Tags To Create – Tags Created And Linked.
    /// </summary>
    [Fact]
    public async Task CreateSpecimen_WithNewTagsToCreate_TagsCreatedAndLinked()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);

        SetupNominatimMock();
        AuthenticateAs(user);

        var request = BuildRequest(species.Guid, tagsToCreate: ["mammal", "endangered"]);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CreatedObjectResponseDto>();
        var tagRelations = await GetTagRelationsForSpecimenFromDbAsync(result!.CreatedObjectGuid);

        tagRelations.Should().HaveCount(2);
    }

    /// <summary>
    /// Tests Create Specimen – With Existing And New Tags – All Tags Linked.
    /// </summary>
    [Fact]
    public async Task CreateSpecimen_WithExistingAndNewTags_AllTagsLinked()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var existingTag = await TagRepo.CreateTagAsync("mammal");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);

        SetupNominatimMock();
        AuthenticateAs(user);

        var request = BuildRequest(species.Guid, tags: [existingTag.Guid], tagsToCreate: ["endangered"]);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CreatedObjectResponseDto>();
        var tagRelations = await GetTagRelationsForSpecimenFromDbAsync(result!.CreatedObjectGuid);

        tagRelations.Should().HaveCount(2);
        tagRelations.Select(r => r.TagGuid).Should().Contain(existingTag.Guid);
    }

    #endregion

    #region CreateSpecimen – Images

    /// <summary>
    /// Tests Create Specimen – With Base64 Image – Image Stored For Specimen.
    /// </summary>
    [Fact]
    public async Task CreateSpecimen_WithBase64Image_ImageStoredForSpecimen()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);

        SetupNominatimMock();
        AuthenticateAs(user);

        var request = BuildRequest(species.Guid, base64Image: "base64image1");

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CreatedObjectResponseDto>();
        var images = await GetImagesForSpecimenFromDbAsync(result!.CreatedObjectGuid);

        images.Should().HaveCount(1);
        images.Single().ImageBase64.Should().Be("base64image1");
    }

    /// <summary>
    /// Tests Create Specimen – Without Image – Specimen Created With No Image.
    /// </summary>
    [Fact]
    public async Task CreateSpecimen_WithoutImage_SpecimenCreatedWithNoImage()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);

        SetupNominatimMock();
        AuthenticateAs(user);

        var request = BuildRequest(species.Guid);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CreatedObjectResponseDto>();
        var images = await GetImagesForSpecimenFromDbAsync(result!.CreatedObjectGuid);

        images.Should().BeEmpty();
    }

    #endregion

    #region CreateSpecimen – Geographic Location

    /// <summary>
    /// Tests Create Specimen – Nominatim Returns Full Address – Creates Complete Geo Chain.
    /// </summary>
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

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, BuildRequest(species.Guid));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var locations = await GetAllGeographicLocationsFromDbAsync();

        locations.Should().HaveCount(5);
        locations.Should().ContainSingle(l => l.Level == GeographicLevel.Country && l.Name == "DE");
        locations.Should().ContainSingle(l => l.Level == GeographicLevel.State && l.Name == "Bayern");
        locations.Should().ContainSingle(l => l.Level == GeographicLevel.City && l.Name == "Munich");
        locations.Should().ContainSingle(l => l.Level == GeographicLevel.Postcode && l.Name == "80331");
        locations.Should().ContainSingle(l => l.Level == GeographicLevel.Street && l.Name == "Marienplatz");
    }

    /// <summary>
    /// Tests Create Specimen – Nominatim Returns Only Country – Creates Only Country Geo Entry.
    /// </summary>
    [Fact]
    public async Task CreateSpecimen_NominatimReturnsOnlyCountry_CreatesOnlyCountryGeoEntry()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);

        SetupNominatimMock(countryCode: "DE", state: null, city: null, postcode: null);

        AuthenticateAs(user);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, BuildRequest(species.Guid));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var locations = await GetAllGeographicLocationsFromDbAsync();

        locations.Should().HaveCount(1);
        locations.Single().Level.Should().Be(GeographicLevel.Country);
        locations.Single().Name.Should().Be("DE");
    }

    /// <summary>
    /// Tests Create Specimen – Specimen Geo Guid Points To Most Specific Level.
    /// </summary>
    [Fact]
    public async Task CreateSpecimen_SpecimenGeoGuidPointsToMostSpecificLevel()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);

        SetupNominatimMock(countryCode: "DE", state: "Bayern", city: "Munich", postcode: null, road: null);

        AuthenticateAs(user);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, BuildRequest(species.Guid));
        var result = await response.Content.ReadFromJsonAsync<CreatedObjectResponseDto>();

        var specimen = await GetSpecimenFromDbAsync(result!.CreatedObjectGuid);
        var locations = await GetAllGeographicLocationsFromDbAsync();

        var cityEntry = locations.Single(l => l.Level == GeographicLevel.City);
        specimen!.GeographicLocationGuid.Should().Be(cityEntry.Guid);
    }

    /// <summary>
    /// Tests Create Specimen – Same Location Submitted Twice – Reuses Existing Geo Entities.
    /// </summary>
    [Fact]
    public async Task CreateSpecimen_SameLocationSubmittedTwice_ReusesExistingGeoEntities()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);

        SetupNominatimMock(countryCode: "DE", state: "Bayern", city: "Munich", postcode: null);

        AuthenticateAs(user);

        var request = BuildRequest(species.Guid);

        await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);
        var locationsAfterFirst = await GetAllGeographicLocationsFromDbAsync();

        await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);
        var locationsAfterSecond = await GetAllGeographicLocationsFromDbAsync();

        locationsAfterSecond.Should().HaveCount(locationsAfterFirst.Count);
    }

    #endregion

    #region CreateSpecimen – Coordinates

    /// <summary>
    /// Tests Create Specimen – Stores Latitude And Longitude On Specimen.
    /// </summary>
    [Fact]
    public async Task CreateSpecimen_StoresLatitudeAndLongitudeOnSpecimen()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);

        SetupNominatimMock();
        AuthenticateAs(user);

        var request = BuildRequest(species.Guid);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);
        var result = await response.Content.ReadFromJsonAsync<CreatedObjectResponseDto>();

        var specimen = await GetSpecimenFromDbAsync(result!.CreatedObjectGuid);
        specimen!.Latitude.Should().BeApproximately(request.Location.Latitude, 0.001);
        specimen.Longitude.Should().BeApproximately(request.Location.Longitude, 0.001);
    }

    #endregion

    #region CreateSpecimen – Status

    /// <summary>
    /// Tests Create Specimen – With Approved Species – Specimen Status Is Active.
    /// </summary>
    [Fact]
    public async Task CreateSpecimen_WithApprovedSpecies_SpecimenStatusIsActive()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species, ApprovalStatus.Approved);

        SetupNominatimMock();
        AuthenticateAs(user);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, BuildRequest(species.Guid));
        var result = await response.Content.ReadFromJsonAsync<CreatedObjectResponseDto>();

        var specimen = await GetSpecimenFromDbAsync(result!.CreatedObjectGuid);
        specimen!.Status.Should().Be(SpecimenStatus.Active);
    }

    /// <summary>
    /// Tests Create Specimen – With Pending Species – Specimen Status Is Pending.
    /// </summary>
    [Fact]
    public async Task CreateSpecimen_WithPendingSpecies_SpecimenStatusIsPending()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species, ApprovalStatus.Pending);

        SetupNominatimMock();
        AuthenticateAs(user);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, BuildRequest(species.Guid));
        var result = await response.Content.ReadFromJsonAsync<CreatedObjectResponseDto>();

        var specimen = await GetSpecimenFromDbAsync(result!.CreatedObjectGuid);
        specimen!.Status.Should().Be(SpecimenStatus.Pending);
    }

    #endregion

    #region CreateSpecimen – Error Cases

    /// <summary>
    /// Tests Create Specimen – Invalid Tag Guid – Returns No Tag With This Guid.
    /// </summary>
    [Fact]
    public async Task CreateSpecimen_InvalidTagGuid_ReturnsNoTagWithThisGuid()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);

        SetupNominatimMock();
        AuthenticateAs(user);

        var request = BuildRequest(species.Guid, tags: [Guid.CreateVersion7()]);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoTagsFound);
    }

    /// <summary>
    /// Tests Create Specimen – Invalid Species Guid – Returns No Taxonomy Found.
    /// </summary>
    [Fact]
    public async Task CreateSpecimen_InvalidSpeciesGuid_ReturnsNoTaxonomyFound()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");

        SetupNominatimMock();
        AuthenticateAs(user);

        var request = BuildRequest(Guid.CreateVersion7());

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoTaxonomyFound);
    }

    /// <summary>
    /// Tests Create Specimen – Species Guid With Non Species Rank – Returns Taxonomy Rank Is No Species.
    /// </summary>
    [Fact]
    public async Task CreateSpecimen_SpeciesGuidWithNonSpeciesRank_ReturnsTaxonomyRankIsNoSpecies()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var genus = await SpecimenRepo.CreateTaxonomyNodeAsync("Panthera", TaxonomyRank.Genus);

        SetupNominatimMock();
        AuthenticateAs(user);

        var request = BuildRequest(genus.Guid);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.TaxonomyRankIsNoSpecies);
    }

    /// <summary>
    /// Tests Create Specimen – Nominatim Returns Null – Returns Address Does Not Exist.
    /// </summary>
    [Fact]
    public async Task CreateSpecimen_NominatimReturnsNull_ReturnsAddressDoesNotExist()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);

        NominatimMock
            .Setup(x => x.SearchLocationAsync(It.IsAny<GeoDto>()))
            .ReturnsAsync((NominatimResponse?)null);

        AuthenticateAs(user);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, BuildRequest(species.Guid));

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.AddressDoesNotExist);
    }

    /// <summary>
    /// Tests Create Specimen – Nominatim Returns Null Country Code – Returns Address Does Not Exist.
    /// </summary>
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

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, BuildRequest(species.Guid));

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.AddressDoesNotExist);
    }

    /// <summary>
    /// Tests Create Specimen – Not Authenticated – Returns Unauthorized.
    /// </summary>
    [Fact]
    public async Task CreateSpecimen_NotAuthenticated_ReturnsUnauthorized()
    {
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, BuildRequest(species.Guid));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region UpdateSpecimen

    /// <summary>
    /// Tests Update Specimen – Valid Data – Returns Ok And Updates Specimen.
    /// </summary>
    [Fact]
    public async Task UpdateSpecimen_ValidData_ReturnsOkAndUpdatesSpecimen()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var request = new UpdateSpecimenRequestDto
        {
            Name = "Updated Name",
            LatinName = "Updated latinname",
            DateOfDiscovery = new DateTime(2020, 5, 1),
            Comment = "New comment",
            IsPublic = false
        };

        var response = await Client.PatchAsJsonAsync($"{Routings.CommonSpecimenRouting}/{specimen.Guid}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await GetSpecimenFromDbAsync(specimen.Guid);
        updated!.Name.Should().Be("Updated Name");
        updated.LatinName.Should().Be("Updated latinname");
        updated.Comment.Should().Be("New comment");
        updated.IsPublic.Should().BeFalse();
    }

    /// <summary>
    /// Tests Update Specimen – No Changes – Returns No Updated Data.
    /// </summary>
    [Fact]
    public async Task UpdateSpecimen_NoChanges_ReturnsNoUpdatedData()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var request = new UpdateSpecimenRequestDto
        {
            Name = specimen.Name,
            LatinName = specimen.LatinName,
            DateOfDiscovery = specimen.DateOfDiscovery,
            Comment = specimen.Comment,
            IsPublic = specimen.IsPublic
        };

        var response = await Client.PatchAsJsonAsync($"{Routings.CommonSpecimenRouting}/{specimen.Guid}", request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoUpdatedData);
    }

    /// <summary>
    /// Tests Update Specimen – Invalid Specimen Guid – Returns No Specimen Found.
    /// </summary>
    [Fact]
    public async Task UpdateSpecimen_InvalidSpecimenGuid_ReturnsNoSpecimenFound()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        AuthenticateAs(user);

        var request = new UpdateSpecimenRequestDto
        {
            Name = "X",
            LatinName = "X",
            DateOfDiscovery = DateTime.UtcNow,
            Comment = null,
            IsPublic = true
        };

        var response = await Client.PatchAsJsonAsync($"{Routings.CommonSpecimenRouting}/{Guid.CreateVersion7()}", request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoSpecimenFound);
    }

    /// <summary>
    /// Tests Update Specimen – As User Not Owner – Returns Forbidden.
    /// </summary>
    [Fact]
    public async Task UpdateSpecimen_AsUserNotOwner_ReturnsForbidden()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var other = await UserRepo.CreateUserAsync("other@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, createdByUserGuid: owner.Guid);

        AuthenticateAs(other);

        var request = new UpdateSpecimenRequestDto
        {
            Name = "Hacked",
            LatinName = "X",
            DateOfDiscovery = DateTime.UtcNow,
            Comment = null,
            IsPublic = true
        };

        var response = await Client.PatchAsJsonAsync($"{Routings.CommonSpecimenRouting}/{specimen.Guid}", request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    /// <summary>
    /// Tests Update Specimen – As Admin – Can Update Other Users Specimen.
    /// </summary>
    [Fact]
    public async Task UpdateSpecimen_AsAdmin_CanUpdateOtherUsersSpecimen()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, createdByUserGuid: owner.Guid);

        AuthenticateAs(admin);

        var request = new UpdateSpecimenRequestDto
        {
            Name = "Admin Updated",
            LatinName = specimen.LatinName,
            DateOfDiscovery = specimen.DateOfDiscovery,
            Comment = "admin fix",
            IsPublic = specimen.IsPublic
        };

        var response = await Client.PatchAsJsonAsync($"{Routings.CommonSpecimenRouting}/{specimen.Guid}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Tests Update Specimen – Not Authenticated – Returns Unauthorized.
    /// </summary>
    [Fact]
    public async Task UpdateSpecimen_NotAuthenticated_ReturnsUnauthorized()
    {
        var request = new UpdateSpecimenRequestDto
        {
            Name = "X", LatinName = "X", DateOfDiscovery = DateTime.UtcNow,
            Comment = null, IsPublic = true
        };
        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonSpecimenRouting}/{Guid.CreateVersion7()}", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Tests Update Specimen – Specimen Is Not Active – Returns Object Is Not Active.
    /// </summary>
    [Fact]
    public async Task UpdateSpecimen_SpecimenIsNotActive_ReturnsObjectIsNotActive()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(
            species.Guid, location.Guid,
            status: SpecimenStatus.Deleted,
            createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var request = new UpdateSpecimenRequestDto
        {
            Name = "Updated", LatinName = "X", DateOfDiscovery = DateTime.UtcNow,
            Comment = null, IsPublic = true
        };

        var response = await Client.PatchAsJsonAsync($"{Routings.CommonSpecimenRouting}/{specimen.Guid}", request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.ObjectIsNotActive);
    }

    #endregion

    #region UpdateSpecimenImage

    /// <summary>
    /// Tests Update Specimen Image – With New Image – Replaces Existing Image.
    /// </summary>
    [Fact]
    public async Task UpdateSpecimenImage_WithNewImage_ReplacesExistingImage()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenWithImageAsync(
            species.Guid, location.Guid, imageBase64: "oldImage", createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var request = new UpdateSpecimenImageRequestDto { ImageBase64 = "newImage" };
        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonSpecimenRouting}/{specimen.Guid}/image", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var images = await GetImagesForSpecimenFromDbAsync(specimen.Guid);
        images.Should().HaveCount(1);
        images.Single().ImageBase64.Should().Be("newImage");
    }

    /// <summary>
    /// Tests Update Specimen Image – With Null Image – Deletes Existing Image.
    /// </summary>
    [Fact]
    public async Task UpdateSpecimenImage_WithNullImage_DeletesExistingImage()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenWithImageAsync(
            species.Guid, location.Guid, imageBase64: "someImage", createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var request = new UpdateSpecimenImageRequestDto { ImageBase64 = null };
        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonSpecimenRouting}/{specimen.Guid}/image", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var images = await GetImagesForSpecimenFromDbAsync(specimen.Guid);
        images.Should().BeEmpty();
    }

    /// <summary>
    /// Tests Update Specimen Image – Specimen Has No Image – Adds New Image.
    /// </summary>
    [Fact]
    public async Task UpdateSpecimenImage_SpecimenHasNoImage_AddsNewImage()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(
            species.Guid, location.Guid, createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var request = new UpdateSpecimenImageRequestDto { ImageBase64 = "brandNew" };
        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonSpecimenRouting}/{specimen.Guid}/image", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var images = await GetImagesForSpecimenFromDbAsync(specimen.Guid);
        images.Should().HaveCount(1);
        images.Single().ImageBase64.Should().Be("brandNew");
    }

    /// <summary>
    /// Tests Update Specimen Image – Same Image Data – Returns No Updated Data.
    /// </summary>
    [Fact]
    public async Task UpdateSpecimenImage_SameImageData_ReturnsNoUpdatedData()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenWithImageAsync(
            species.Guid, location.Guid, imageBase64: "same", createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var request = new UpdateSpecimenImageRequestDto { ImageBase64 = "same" };
        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonSpecimenRouting}/{specimen.Guid}/image", request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoUpdatedData);
    }

    /// <summary>
    /// Tests Update Specimen Image – Invalid Specimen Guid – Returns No Specimen Found.
    /// </summary>
    [Fact]
    public async Task UpdateSpecimenImage_InvalidSpecimenGuid_ReturnsNoSpecimenFound()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        AuthenticateAs(user);

        var request = new UpdateSpecimenImageRequestDto { ImageBase64 = "data" };
        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonSpecimenRouting}/{Guid.CreateVersion7()}/image", request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoSpecimenFound);
    }

    /// <summary>
    /// Tests Update Specimen Image – As User Not Owner – Returns Forbidden.
    /// </summary>
    [Fact]
    public async Task UpdateSpecimenImage_AsUserNotOwner_ReturnsForbidden()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var other = await UserRepo.CreateUserAsync("other@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenWithImageAsync(
            species.Guid, location.Guid, createdByUserGuid: owner.Guid);

        AuthenticateAs(other);

        var request = new UpdateSpecimenImageRequestDto { ImageBase64 = "hacked" };
        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonSpecimenRouting}/{specimen.Guid}/image", request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    /// <summary>
    /// Tests Update Specimen Image – Not Authenticated – Returns Unauthorized.
    /// </summary>
    [Fact]
    public async Task UpdateSpecimenImage_NotAuthenticated_ReturnsUnauthorized()
    {
        var request = new UpdateSpecimenImageRequestDto { ImageBase64 = "data" };
        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonSpecimenRouting}/{Guid.CreateVersion7()}/image", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Tests Update Specimen Image – Specimen Is Not Active – Returns Object Is Not Active.
    /// </summary>
    [Fact]
    public async Task UpdateSpecimenImage_SpecimenIsNotActive_ReturnsObjectIsNotActive()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(
            species.Guid, location.Guid,
            status: SpecimenStatus.Deleted,
            createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var request = new UpdateSpecimenImageRequestDto { ImageBase64 = "new-image" };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonSpecimenRouting}/{specimen.Guid}/image", request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.ObjectIsNotActive);
    }

    #endregion

    #region UpdateSpecimenTags

    /// <summary>
    /// Tests Update Specimen Tags – Replace With Existing Tags – Updates Tag Links.
    /// </summary>
    [Fact]
    public async Task UpdateSpecimenTags_ReplaceWithExistingTags_UpdatesTagLinks()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        var tag1 = await TagRepo.CreateTagAsync("mammal");
        var tag2 = await TagRepo.CreateTagAsync("endangered");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenWithTagsAsync(
            species.Guid, location.Guid, [tag1], createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var request = new UpdateSpecimenTagsRequestDto
        {
            SpecimenTags =
            [
                new UpdateTagsDto { TagGuid = tag2.Guid, TagName = tag2.Tag }
            ]
        };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonSpecimenRouting}/{specimen.Guid}/tags", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var relations = await GetTagRelationsForSpecimenFromDbAsync(specimen.Guid);
        relations.Should().HaveCount(1);
        relations.Single().TagGuid.Should().Be(tag2.Guid);
    }

    /// <summary>
    /// Tests Update Specimen Tags – With New Tags To Create – Creates And Links New Tags.
    /// </summary>
    [Fact]
    public async Task UpdateSpecimenTags_WithNewTagsToCreate_CreatesAndLinksNewTags()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        var existingTag = await TagRepo.CreateTagAsync("mammal");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenWithTagsAsync(
            species.Guid, location.Guid, [existingTag], createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var request = new UpdateSpecimenTagsRequestDto
        {
            SpecimenTags =
            [
                new UpdateTagsDto { TagGuid = existingTag.Guid, TagName = existingTag.Tag },
                new UpdateTagsDto { TagGuid = null, TagName = "newTag" }
            ]
        };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonSpecimenRouting}/{specimen.Guid}/tags", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var relations = await GetTagRelationsForSpecimenFromDbAsync(specimen.Guid);
        relations.Should().HaveCount(2);
    }

    /// <summary>
    /// Tests Update Specimen Tags – Same Tags – Returns No Updated Data.
    /// </summary>
    [Fact]
    public async Task UpdateSpecimenTags_SameTags_ReturnsNoUpdatedData()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        var tag = await TagRepo.CreateTagAsync("mammal");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenWithTagsAsync(
            species.Guid, location.Guid, [tag], createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var request = new UpdateSpecimenTagsRequestDto
        {
            SpecimenTags = [new UpdateTagsDto { TagGuid = tag.Guid, TagName = tag.Tag }]
        };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonSpecimenRouting}/{specimen.Guid}/tags", request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoUpdatedData);
    }

    /// <summary>
    /// Tests Update Specimen Tags – Remove All Tags – Removes All Tag Links.
    /// </summary>
    [Fact]
    public async Task UpdateSpecimenTags_RemoveAllTags_RemovesAllTagLinks()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        var tag = await TagRepo.CreateTagAsync("mammal");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenWithTagsAsync(
            species.Guid, location.Guid, [tag], createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var request = new UpdateSpecimenTagsRequestDto { SpecimenTags = [] };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonSpecimenRouting}/{specimen.Guid}/tags", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var relations = await GetTagRelationsForSpecimenFromDbAsync(specimen.Guid);
        relations.Should().BeEmpty();
    }

    /// <summary>
    /// Tests Update Specimen Tags – Invalid Specimen Guid – Returns No Specimen Found.
    /// </summary>
    [Fact]
    public async Task UpdateSpecimenTags_InvalidSpecimenGuid_ReturnsNoSpecimenFound()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        AuthenticateAs(user);

        var request = new UpdateSpecimenTagsRequestDto { SpecimenTags = [] };
        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonSpecimenRouting}/{Guid.CreateVersion7()}/tags", request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoSpecimenFound);
    }

    /// <summary>
    /// Tests Update Specimen Tags – As User Not Owner – Returns Forbidden.
    /// </summary>
    [Fact]
    public async Task UpdateSpecimenTags_AsUserNotOwner_ReturnsForbidden()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var other = await UserRepo.CreateUserAsync("other@test.de");
        var tag = await TagRepo.CreateTagAsync("mammal");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenWithTagsAsync(
            species.Guid, location.Guid, [tag], createdByUserGuid: owner.Guid);

        AuthenticateAs(other);

        var request = new UpdateSpecimenTagsRequestDto
        {
            SpecimenTags = [new UpdateTagsDto { TagGuid = null, TagName = "hacked" }]
        };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonSpecimenRouting}/{specimen.Guid}/tags", request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    /// <summary>
    /// Tests Update Specimen Tags – Not Authenticated – Returns Unauthorized.
    /// </summary>
    [Fact]
    public async Task UpdateSpecimenTags_NotAuthenticated_ReturnsUnauthorized()
    {
        var request = new UpdateSpecimenTagsRequestDto { SpecimenTags = [] };
        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonSpecimenRouting}/{Guid.CreateVersion7()}/tags", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Tests Update Specimen Tags – Specimen Is Not Active – Returns Object Is Not Active.
    /// </summary>
    [Fact]
    public async Task UpdateSpecimenTags_SpecimenIsNotActive_ReturnsObjectIsNotActive()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(
            species.Guid, location.Guid,
            status: SpecimenStatus.Deleted,
            createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var request = new UpdateSpecimenTagsRequestDto { SpecimenTags = [] };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonSpecimenRouting}/{specimen.Guid}/tags", request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.ObjectIsNotActive);
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
        Guid taxonomySpeciesGuid,
        string name               = "Test Specimen",
        string latinName          = "Testus specienus",
        DateTime? dateOfDiscovery = null,
        string? comment           = null,
        string? storageBuilding   = null,
        string? storageCabinet    = null,
        string? storageShelf      = null,
        string? storageCompartment = null,
        List<Guid>? tags          = null,
        List<string>? tagsToCreate = null,
        string? base64Image       = null) => new()
    {
        Name               = name,
        LatinName          = latinName,
        DateOfDiscovery    = dateOfDiscovery ?? DateTime.UtcNow,
        Location           = new GeoDto { Latitude = 48.137, Longitude = 11.575 },
        TaxonomySpeciesGuid = taxonomySpeciesGuid,
        Comment            = comment,
        StorageBuilding    = storageBuilding,
        StorageCabinet     = storageCabinet,
        StorageShelf       = storageShelf,
        StorageCompartment = storageCompartment,
        Tags               = tags ?? [],
        TagsToCreate       = tagsToCreate ?? [],
        Base64Image        = base64Image,
    };

    #endregion

    #region GetSpecimenDetail

    /// <summary>
    /// Tests Get Specimen Detail – Public Specimen In Dynamic Collection – Returns Detail.
    /// </summary>
    [Fact]
    public async Task GetSpecimenDetail_PublicSpecimenInDynamicCollection_ReturnsDetail()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var collection = await CollectionRepo.CreateCollectionAsync(isPrivate: false, createdByUserGuid: owner.Guid);
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true, createdByUserGuid: owner.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, specimen.Guid);

        var viewer = await UserRepo.CreateUserAsync("viewer@test.de");
        AuthenticateAs(viewer);

        var response = await Client.GetAsync(
            $"{Routings.CommonSpecimenRouting}/{collection.Guid}/{specimen.Guid}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetSpecimenDetailResponseDto>();
        result!.SpecimenGuid.Should().Be(specimen.Guid);
        result.IsPublic.Should().BeTrue();
    }

    /// <summary>
    /// Tests Get Specimen Detail – Returns All Core Fields.
    /// </summary>
    [Fact]
    public async Task GetSpecimenDetail_ReturnsAllCoreFields()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync("DE", GeographicLevel.Country);
        var collection = await CollectionRepo.CreateCollectionAsync(isPrivate: false, createdByUserGuid: owner.Guid);
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true, createdByUserGuid: owner.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, specimen.Guid);

        AuthenticateAs(owner);

        var result = await Client.GetFromJsonAsync<GetSpecimenDetailResponseDto>(
            $"{Routings.CommonSpecimenRouting}/{collection.Guid}/{specimen.Guid}");

        result!.SpecimenGuid.Should().Be(specimen.Guid);
        result.Name.Should().Be(specimen.Name);
        result.LatinName.Should().Be(specimen.LatinName);
        result.Status.Should().Be(specimen.Status);
        result.IsPublic.Should().Be(specimen.IsPublic);
        result.CreatedAt.Should().NotBeNull();
    }

    /// <summary>
    /// Tests Get Specimen Detail – Taxonomy Path Ordered By Rank Ascending.
    /// </summary>
    [Fact]
    public async Task GetSpecimenDetail_TaxonomyPathOrderedByRankAscending()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var phylum  = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata",  TaxonomyRank.Phylum);
        var @class  = await SpecimenRepo.CreateTaxonomyNodeAsync("Mammalia",  TaxonomyRank.Class,  parentGuid: phylum.Guid);
        var order   = await SpecimenRepo.CreateTaxonomyNodeAsync("Carnivora", TaxonomyRank.Order,  parentGuid: @class.Guid);
        var family  = await SpecimenRepo.CreateTaxonomyNodeAsync("Felidae",   TaxonomyRank.Family, parentGuid: order.Guid);
        var genus   = await SpecimenRepo.CreateTaxonomyNodeAsync("Panthera",  TaxonomyRank.Genus,  parentGuid: family.Guid);
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo",       TaxonomyRank.Species, parentGuid: genus.Guid);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var collection = await CollectionRepo.CreateCollectionAsync(isPrivate: false, createdByUserGuid: owner.Guid);
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true, createdByUserGuid: owner.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, specimen.Guid);

        AuthenticateAs(owner);

        var result = await Client.GetFromJsonAsync<GetSpecimenDetailResponseDto>(
            $"{Routings.CommonSpecimenRouting}/{collection.Guid}/{specimen.Guid}");

        result!.TaxonomyPath.Should().HaveCount(6);
        result.TaxonomyPath.Should().BeInAscendingOrder(n => n.Rank);
        result.TaxonomyPath[0].TaxonomyGuid.Should().Be(phylum.Guid);
        result.TaxonomyPath[^1].TaxonomyGuid.Should().Be(species.Guid);
    }

    /// <summary>
    /// Tests Get Specimen Detail – Geo Path Ordered Root To Leaf.
    /// </summary>
    [Fact]
    public async Task GetSpecimenDetail_GeoPathOrderedRootToLeaf()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var country = await SpecimenRepo.CreateGeographicLocationAsync("DE", GeographicLevel.Country);
        var city    = await SpecimenRepo.CreateGeographicLocationAsync("Munich", GeographicLevel.City, parentGuid: country.Guid);
        var collection = await CollectionRepo.CreateCollectionAsync(isPrivate: false, createdByUserGuid: owner.Guid);
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, city.Guid, isPublic: true, createdByUserGuid: owner.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, specimen.Guid);

        AuthenticateAs(owner);

        var result = await Client.GetFromJsonAsync<GetSpecimenDetailResponseDto>(
            $"{Routings.CommonSpecimenRouting}/{collection.Guid}/{specimen.Guid}");

        result!.GeoPath.Should().HaveCount(2);
        result.GeoPath[0].Level.Should().Be(GeographicLevel.Country);
        result.GeoPath[0].Name.Should().Be("DE");
        result.GeoPath[1].Level.Should().Be(GeographicLevel.City);
        result.GeoPath[1].Name.Should().Be("Munich");
    }

    /// <summary>
    /// Tests Get Specimen Detail – Own Private Specimen – Returns Detail.
    /// </summary>
    [Fact]
    public async Task GetSpecimenDetail_OwnPrivateSpecimen_ReturnsDetail()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: false, createdByUserGuid: owner.Guid);

        AuthenticateAs(owner);

        var response = await Client.GetAsync(
            $"{Routings.CommonSpecimenRouting}/{AllSpecimensGuid}/{specimen.Guid}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Tests Get Specimen Detail – Other Users Private Specimen – Returns Forbidden.
    /// </summary>
    [Fact]
    public async Task GetSpecimenDetail_OtherUsersPrivateSpecimen_ReturnsForbidden()
    {
        var owner  = await UserRepo.CreateUserAsync("owner@test.de");
        var viewer = await UserRepo.CreateUserAsync("viewer@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: false, createdByUserGuid: owner.Guid);

        AuthenticateAs(viewer);

        var response = await Client.GetAsync(
            $"{Routings.CommonSpecimenRouting}/{AllSpecimensGuid}/{specimen.Guid}");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    /// <summary>
    /// Tests Get Specimen Detail – Admin Can See Private Specimen Of Other User.
    /// </summary>
    [Fact]
    public async Task GetSpecimenDetail_AdminCanSeePrivateSpecimenOfOtherUser()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: false, createdByUserGuid: owner.Guid);

        AuthenticateAs(admin);

        var response = await Client.GetAsync(
            $"{Routings.CommonSpecimenRouting}/{AllSpecimensGuid}/{specimen.Guid}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Tests Get Specimen Detail – Specimen Not In Dynamic Collection – Returns Specimen Not In Collection.
    /// </summary>
    [Fact]
    public async Task GetSpecimenDetail_SpecimenNotInDynamicCollection_ReturnsSpecimenNotInCollection()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var collection = await CollectionRepo.CreateCollectionAsync(isPrivate: false, createdByUserGuid: owner.Guid);
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true, createdByUserGuid: owner.Guid);

        AuthenticateAs(owner);

        var response = await Client.GetAsync(
            $"{Routings.CommonSpecimenRouting}/{collection.Guid}/{specimen.Guid}");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.SpecimenNotInCollection);
    }

    /// <summary>
    /// Tests Get Specimen Detail – Static Collection – All Specimens – Always Allowed.
    /// </summary>
    [Fact]
    public async Task GetSpecimenDetail_StaticCollection_AllSpecimens_AlwaysAllowed()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true, createdByUserGuid: owner.Guid);

        AuthenticateAs(owner);

        var response = await Client.GetAsync(
            $"{Routings.CommonSpecimenRouting}/{AllSpecimensGuid}/{specimen.Guid}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Tests Get Specimen Detail – Static Collection – My Active – Returns Specimen Not In Collection For Wrong Status.
    /// </summary>
    [Fact]
    public async Task GetSpecimenDetail_StaticCollection_MyActive_ReturnsSpecimenNotInCollectionForWrongStatus()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, SpecimenStatus.Pending, createdByUserGuid: owner.Guid);

        AuthenticateAs(owner);

        var response = await Client.GetAsync(
            $"{Routings.CommonSpecimenRouting}/{MyActiveGuid}/{specimen.Guid}");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.SpecimenNotInCollection);
    }

    /// <summary>
    /// Tests Get Specimen Detail – Static Collection – My Collection – Other User Specimen – Returns Specimen Not In Collection.
    /// </summary>
    [Fact]
    public async Task GetSpecimenDetail_StaticCollection_MyCollection_OtherUserSpecimen_ReturnsSpecimenNotInCollection()
    {
        var owner  = await UserRepo.CreateUserAsync("owner@test.de");
        var other  = await UserRepo.CreateUserAsync("other@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true, createdByUserGuid: owner.Guid);

        AuthenticateAs(other);

        var response = await Client.GetAsync(
            $"{Routings.CommonSpecimenRouting}/{MyCollectionGuid}/{specimen.Guid}");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.SpecimenNotInCollection);
    }

    /// <summary>
    /// Tests Get Specimen Detail – Unknown Specimen Guid – Returns No Specimen Found.
    /// </summary>
    [Fact]
    public async Task GetSpecimenDetail_UnknownSpecimenGuid_ReturnsNoSpecimenFound()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var response = await Client.GetAsync(
            $"{Routings.CommonSpecimenRouting}/{AllSpecimensGuid}/{Guid.CreateVersion7()}");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoSpecimenFound);
    }

    #endregion

    #region DeleteSpecimen

    /// <summary>
    /// Tests Delete Specimen – As Owner – Soft Deletes Specimen.
    /// </summary>
    [Fact]
    public async Task DeleteSpecimen_AsOwner_SoftDeletesSpecimen()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, createdByUserGuid: owner.Guid);

        AuthenticateAs(owner);

        var response = await Client.DeleteAsync(
            $"{Routings.CommonSpecimenRouting}/{specimen.Guid}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleted = await GetSpecimenFromDbAsync(specimen.Guid);
        deleted.Should().NotBeNull();
        deleted!.DeletedByUserGuid.Should().Be(owner.Guid);
        deleted.DeletionDate.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
        deleted.Status.Should().Be(SpecimenStatus.Deleted);
    }

    /// <summary>
    /// Tests Delete Specimen – As Admin – Can Soft Delete Any Specimen.
    /// </summary>
    [Fact]
    public async Task DeleteSpecimen_AsAdmin_CanSoftDeleteAnySpecimen()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, createdByUserGuid: owner.Guid);

        AuthenticateAs(admin);

        var response = await Client.DeleteAsync(
            $"{Routings.CommonSpecimenRouting}/{specimen.Guid}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleted = await GetSpecimenFromDbAsync(specimen.Guid);
        deleted.Should().NotBeNull();
        deleted!.DeletedByUserGuid.Should().Be(admin.Guid);
        deleted.DeletionDate.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
        deleted.Status.Should().Be(SpecimenStatus.Deleted);
    }

    /// <summary>
    /// Tests Delete Specimen – As User On Other Users Specimen – Returns Forbidden.
    /// </summary>
    [Fact]
    public async Task DeleteSpecimen_AsUserOnOtherUsersSpecimen_ReturnsForbidden()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var other = await UserRepo.CreateUserAsync("other@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, createdByUserGuid: owner.Guid);

        AuthenticateAs(other);

        var response = await Client.DeleteAsync(
            $"{Routings.CommonSpecimenRouting}/{specimen.Guid}");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    /// <summary>
    /// Tests Delete Specimen – Specimen Not Found – Returns No Specimen Found.
    /// </summary>
    [Fact]
    public async Task DeleteSpecimen_SpecimenNotFound_ReturnsNoSpecimenFound()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var response = await Client.DeleteAsync(
            $"{Routings.CommonSpecimenRouting}/{Guid.CreateVersion7()}");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoSpecimenFound);
    }

    /// <summary>
    /// Tests Delete Specimen – Not Authenticated – Returns Unauthorized.
    /// </summary>
    [Fact]
    public async Task DeleteSpecimen_NotAuthenticated_ReturnsUnauthorized()
    {
        var response = await Client.DeleteAsync(
            $"{Routings.CommonSpecimenRouting}/{Guid.CreateVersion7()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region ActivateSpecimen

    /// <summary>
    /// Tests Activate Specimen – As Owner – Activates Specimen.
    /// </summary>
    [Fact]
    public async Task ActivateSpecimen_AsOwner_ActivatesSpecimen()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(
            species.Guid, location.Guid, SpecimenStatus.Deleted, createdByUserGuid: owner.Guid);

        AuthenticateAs(owner);

        var response = await Client.PostAsync(
            $"{Routings.CommonSpecimenRouting}/{specimen.Guid}/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var activated = await GetSpecimenFromDbAsync(specimen.Guid);
        activated!.DeletionDate.Should().BeNull();
        activated.DeletedByUserGuid.Should().BeNull();
        activated.UpdatedByUserGuid.Should().Be(owner.Guid);
        activated.Status.Should().Be(SpecimenStatus.Active);
    }

    /// <summary>
    /// Tests Activate Specimen – As Admin – Can Activate Any Specimen.
    /// </summary>
    [Fact]
    public async Task ActivateSpecimen_AsAdmin_CanActivateAnySpecimen()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(
            species.Guid, location.Guid, SpecimenStatus.Deleted, createdByUserGuid: owner.Guid);

        AuthenticateAs(admin);

        var response = await Client.PostAsync(
            $"{Routings.CommonSpecimenRouting}/{specimen.Guid}/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var activated = await GetSpecimenFromDbAsync(specimen.Guid);
        activated!.DeletionDate.Should().BeNull();
        activated.DeletedByUserGuid.Should().BeNull();
        activated.UpdatedByUserGuid.Should().Be(admin.Guid);
        activated.Status.Should().Be(SpecimenStatus.Active);
    }

    /// <summary>
    /// Tests Activate Specimen – As User On Other Users Specimen – Returns Forbidden.
    /// </summary>
    [Fact]
    public async Task ActivateSpecimen_AsUserOnOtherUsersSpecimen_ReturnsForbidden()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var other = await UserRepo.CreateUserAsync("other@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(
            species.Guid, location.Guid, SpecimenStatus.Deleted, createdByUserGuid: owner.Guid);

        AuthenticateAs(other);

        var response = await Client.PostAsync(
            $"{Routings.CommonSpecimenRouting}/{specimen.Guid}/activate", null);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    /// <summary>
    /// Tests Activate Specimen – Specimen Not Found – Returns No Specimen Found.
    /// </summary>
    [Fact]
    public async Task ActivateSpecimen_SpecimenNotFound_ReturnsNoSpecimenFound()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var response = await Client.PostAsync(
            $"{Routings.CommonSpecimenRouting}/{Guid.CreateVersion7()}/activate", null);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoSpecimenFound);
    }

    /// <summary>
    /// Tests Activate Specimen – Not Authenticated – Returns Unauthorized.
    /// </summary>
    [Fact]
    public async Task ActivateSpecimen_NotAuthenticated_ReturnsUnauthorized()
    {
        var response = await Client.PostAsync(
            $"{Routings.CommonSpecimenRouting}/{Guid.CreateVersion7()}/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GetSpecimenDetail – Anonymous

    /// <summary>
    /// Tests Get Specimen Detail – Anonymous – Public Specimen – Returns Detail.
    /// </summary>
    [Fact]
    public async Task GetSpecimenDetail_Anonymous_PublicSpecimen_ReturnsDetail()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var collection = await CollectionRepo.CreateCollectionAsync(isPrivate: false, createdByUserGuid: owner.Guid);
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true, createdByUserGuid: owner.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, specimen.Guid);

        var response = await Client.GetAsync(
            $"{Routings.CommonSpecimenRouting}/{collection.Guid}/{specimen.Guid}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetSpecimenDetailResponseDto>();
        result!.SpecimenGuid.Should().Be(specimen.Guid);
        result.IsPublic.Should().BeTrue();
    }

    /// <summary>
    /// Tests Get Specimen Detail – Anonymous – Private Specimen – Returns Forbidden.
    /// </summary>
    [Fact]
    public async Task GetSpecimenDetail_Anonymous_PrivateSpecimen_ReturnsForbidden()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: false, createdByUserGuid: owner.Guid);

        var response = await Client.GetAsync(
            $"{Routings.CommonSpecimenRouting}/{AllSpecimensGuid}/{specimen.Guid}");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    /// <summary>
    /// Tests Get Specimen Detail – Anonymous – Specimen Not In Collection – Returns Specimen Not In Collection.
    /// </summary>
    [Fact]
    public async Task GetSpecimenDetail_Anonymous_SpecimenNotInCollection_ReturnsSpecimenNotInCollection()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var collection = await CollectionRepo.CreateCollectionAsync(isPrivate: false, createdByUserGuid: owner.Guid);
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true, createdByUserGuid: owner.Guid);

        var response = await Client.GetAsync(
            $"{Routings.CommonSpecimenRouting}/{collection.Guid}/{specimen.Guid}");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.SpecimenNotInCollection);
    }

    /// <summary>
    /// Tests Get Specimen Detail – Anonymous – Taxonomy Path Present For Public Specimen.
    /// </summary>
    [Fact]
    public async Task GetSpecimenDetail_Anonymous_TaxonomyPathPresentForPublicSpecimen()
    {
        var owner   = await UserRepo.CreateUserAsync("owner@test.de");
        var phylum  = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata",  TaxonomyRank.Phylum);
        var @class  = await SpecimenRepo.CreateTaxonomyNodeAsync("Mammalia",  TaxonomyRank.Class, parentGuid: phylum.Guid);
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo",       TaxonomyRank.Species, parentGuid: @class.Guid);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var collection = await CollectionRepo.CreateCollectionAsync(isPrivate: false, createdByUserGuid: owner.Guid);
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true, createdByUserGuid: owner.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, specimen.Guid);

        var result = await Client.GetFromJsonAsync<GetSpecimenDetailResponseDto>(
            $"{Routings.CommonSpecimenRouting}/{collection.Guid}/{specimen.Guid}");

        result!.TaxonomyPath.Should().HaveCount(3);
        result.TaxonomyPath.Should().BeInAscendingOrder(n => n.Rank);
    }

    /// <summary>
    /// Tests Get Specimen Detail – Logged In User – Can Access Own Private Specimen.
    /// </summary>
    [Fact]
    public async Task GetSpecimenDetail_LoggedInUser_CanAccessOwnPrivateSpecimen()
    {
        var owner    = await UserRepo.CreateUserAsync("owner@test.de");
        var species  = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: false, createdByUserGuid: owner.Guid);

        AuthenticateAs(owner);

        var response = await Client.GetAsync(
            $"{Routings.CommonSpecimenRouting}/{AllSpecimensGuid}/{specimen.Guid}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetSpecimenDetailResponseDto>();
        result!.SpecimenGuid.Should().Be(specimen.Guid);
    }

    /// <summary>
    /// Tests Get Specimen Detail – Logged In User – Cannot Access Other Users Private Specimen.
    /// </summary>
    [Fact]
    public async Task GetSpecimenDetail_LoggedInUser_CannotAccessOtherUsersPrivateSpecimen()
    {
        var owner   = await UserRepo.CreateUserAsync("owner@test.de");
        var viewer  = await UserRepo.CreateUserAsync("viewer@test.de");
        var species  = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: false, createdByUserGuid: owner.Guid);

        AuthenticateAs(viewer);

        var response = await Client.GetAsync(
            $"{Routings.CommonSpecimenRouting}/{AllSpecimensGuid}/{specimen.Guid}");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    #endregion

    #region GetSpecimenByTaxonomy

    /// <summary>
    /// Tests Get Specimen By Taxonomy – Specimen With Tags – Returns Tags With Guid And Name.
    /// </summary>
    [Fact]
    public async Task GetSpecimenByTaxonomy_SpecimenWithTags_ReturnsTagsWithGuidAndName()
    {
        var owner    = await UserRepo.CreateUserAsync("owner@test.de");
        var species  = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var tag1 = await TagRepo.CreateTagAsync("Big");
        var tag2 = await TagRepo.CreateTagAsync("Blue");
        var specimen = await SpecimenRepo.CreateSpecimenWithTagsAsync(
            species.Guid, location.Guid, [tag1, tag2], createdByUserGuid: owner.Guid);

        var result = await Client.GetFromJsonAsync<List<SpecimenWithTagsDto>>(
            $"{Routings.CommonSpecimenRouting}/{species.Guid}");

        var returned = result!.Single(s => s.SpecimenGuid == specimen.Guid);
        returned.Tags.Should().HaveCount(2);
        returned.Tags.Select(t => t.TagName).Should().BeEquivalentTo("Big", "Blue");
        returned.Tags.Select(t => t.TagGuid).Should().BeEquivalentTo([tag1.Guid, tag2.Guid]);
    }

    /// <summary>
    /// Tests Get Specimen By Taxonomy – Specimen Without Tags – Returns Empty Tag List.
    /// </summary>
    [Fact]
    public async Task GetSpecimenByTaxonomy_SpecimenWithoutTags_ReturnsEmptyTagList()
    {
        var owner    = await UserRepo.CreateUserAsync("owner@test.de");
        var species  = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, createdByUserGuid: owner.Guid);

        var result = await Client.GetFromJsonAsync<List<SpecimenWithTagsDto>>(
            $"{Routings.CommonSpecimenRouting}/{species.Guid}");

        result!.Single().Tags.Should().BeEmpty();
    }

    /// <summary>
    /// Tests Get Specimen By Taxonomy – Species Node – Returns Linked Specimens.
    /// </summary>
    [Fact]
    public async Task GetSpecimenByTaxonomy_SpeciesNode_ReturnsLinkedSpecimens()
    {
        var owner    = await UserRepo.CreateUserAsync("owner@test.de");
        var species  = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen1 = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, createdByUserGuid: owner.Guid);
        var specimen2 = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, createdByUserGuid: owner.Guid);

        var result = await Client.GetFromJsonAsync<List<SpecimenWithTagsDto>>(
            $"{Routings.CommonSpecimenRouting}/{species.Guid}");

        result!.Should().HaveCount(2);
        result.Select(s => s.SpecimenGuid).Should().Contain(specimen1.Guid);
        result.Select(s => s.SpecimenGuid).Should().Contain(specimen2.Guid);
    }

    /// <summary>
    /// Tests Get Specimen By Taxonomy – Genus Node – Returns Specimens From All Child Species.
    /// </summary>
    [Fact]
    public async Task GetSpecimenByTaxonomy_GenusNode_ReturnsSpecimensFromAllChildSpecies()
    {
        var owner    = await UserRepo.CreateUserAsync("owner@test.de");
        var genus    = await SpecimenRepo.CreateTaxonomyNodeAsync("Panthera", TaxonomyRank.Genus);
        var species1 = await SpecimenRepo.CreateTaxonomyNodeAsync("leo",     TaxonomyRank.Species, parentGuid: genus.Guid);
        var species2 = await SpecimenRepo.CreateTaxonomyNodeAsync("tigris",  TaxonomyRank.Species, parentGuid: genus.Guid);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen1 = await SpecimenRepo.CreateSpecimenAsync(species1.Guid, location.Guid, createdByUserGuid: owner.Guid);
        var specimen2 = await SpecimenRepo.CreateSpecimenAsync(species2.Guid, location.Guid, createdByUserGuid: owner.Guid);

        var result = await Client.GetFromJsonAsync<List<SpecimenWithTagsDto>>(
            $"{Routings.CommonSpecimenRouting}/{genus.Guid}");

        result!.Should().HaveCount(2);
        result.Select(s => s.SpecimenGuid).Should().Contain(specimen1.Guid);
        result.Select(s => s.SpecimenGuid).Should().Contain(specimen2.Guid);
    }

    /// <summary>
    /// Tests Get Specimen By Taxonomy – Higher Rank Node – Returns Specimens Across Full Subtree.
    /// </summary>
    [Fact]
    public async Task GetSpecimenByTaxonomy_HigherRankNode_ReturnsSpecimensAcrossFullSubtree()
    {
        var owner   = await UserRepo.CreateUserAsync("owner@test.de");
        var phylum  = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata",  TaxonomyRank.Phylum);
        var @class  = await SpecimenRepo.CreateTaxonomyNodeAsync("Mammalia",  TaxonomyRank.Class,   parentGuid: phylum.Guid);
        var family  = await SpecimenRepo.CreateTaxonomyNodeAsync("Felidae",   TaxonomyRank.Family,  parentGuid: @class.Guid);
        var genus   = await SpecimenRepo.CreateTaxonomyNodeAsync("Panthera",  TaxonomyRank.Genus,   parentGuid: family.Guid);
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo",       TaxonomyRank.Species, parentGuid: genus.Guid);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, createdByUserGuid: owner.Guid);

        var result = await Client.GetFromJsonAsync<List<SpecimenWithTagsDto>>(
            $"{Routings.CommonSpecimenRouting}/{phylum.Guid}");

        result!.Should().HaveCount(1);
        result.Single().SpecimenGuid.Should().Be(specimen.Guid);
    }

    /// <summary>
    /// Tests Get Specimen By Taxonomy – Only Returns Specimens From Subtree – Not From Sibling Branch.
    /// </summary>
    [Fact]
    public async Task GetSpecimenByTaxonomy_OnlyReturnsSpecimensFromSubtree_NotFromSiblingBranch()
    {
        var owner       = await UserRepo.CreateUserAsync("owner@test.de");
        var order       = await SpecimenRepo.CreateTaxonomyNodeAsync("Carnivora", TaxonomyRank.Order);
        var familyFeli  = await SpecimenRepo.CreateTaxonomyNodeAsync("Felidae",   TaxonomyRank.Family,  parentGuid: order.Guid);
        var familyCanid = await SpecimenRepo.CreateTaxonomyNodeAsync("Canidae",   TaxonomyRank.Family,  parentGuid: order.Guid);
        var speciesCat  = await SpecimenRepo.CreateTaxonomyNodeAsync("leo",       TaxonomyRank.Species, parentGuid: familyFeli.Guid);
        var speciesDog  = await SpecimenRepo.CreateTaxonomyNodeAsync("lupus",     TaxonomyRank.Species, parentGuid: familyCanid.Guid);
        var location    = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimenCat = await SpecimenRepo.CreateSpecimenAsync(speciesCat.Guid, location.Guid, createdByUserGuid: owner.Guid);
        var specimenDog = await SpecimenRepo.CreateSpecimenAsync(speciesDog.Guid, location.Guid, createdByUserGuid: owner.Guid);

        var result = await Client.GetFromJsonAsync<List<SpecimenWithTagsDto>>(
            $"{Routings.CommonSpecimenRouting}/{familyFeli.Guid}");

        result!.Should().HaveCount(1);
        result.Single().SpecimenGuid.Should().Be(specimenCat.Guid);
        result.Select(s => s.SpecimenGuid).Should().NotContain(specimenDog.Guid);
    }

    /// <summary>
    /// Tests Get Specimen By Taxonomy – Node With No Descendant Specimens – Returns Empty List.
    /// </summary>
    [Fact]
    public async Task GetSpecimenByTaxonomy_NodeWithNoDescendantSpecimens_ReturnsEmptyList()
    {
        var genus = await SpecimenRepo.CreateTaxonomyNodeAsync("Panthera", TaxonomyRank.Genus);
        await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species, parentGuid: genus.Guid);

        var result = await Client.GetFromJsonAsync<List<SpecimenWithTagsDto>>(
            $"{Routings.CommonSpecimenRouting}/{genus.Guid}");

        result!.Should().BeEmpty();
    }

    /// <summary>
    /// Tests Get Specimen By Taxonomy – Unknown Guid – Returns No Taxonomy Found.
    /// </summary>
    [Fact]
    public async Task GetSpecimenByTaxonomy_UnknownGuid_ReturnsNoTaxonomyFound()
    {
        var response = await Client.GetAsync(
            $"{Routings.CommonSpecimenRouting}/{Guid.CreateVersion7()}");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoTaxonomyFound);
    }

    #endregion

    #region GetSpecimenByTaxonomy – Visibility

    /// <summary>
    /// Tests Get Specimen By Taxonomy – As Anonymous – Does Not Return Private Specimens.
    /// </summary>
    [Fact]
    public async Task GetSpecimenByTaxonomy_AsAnonymous_DoesNotReturnPrivateSpecimens()
    {
        var owner    = await UserRepo.CreateUserAsync("owner@test.de");
        var species  = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var publicSpecimen  = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true,  createdByUserGuid: owner.Guid);
        var privateSpecimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: false, createdByUserGuid: owner.Guid);

        var result = await Client.GetFromJsonAsync<List<SpecimenWithTagsDto>>(
            $"{Routings.CommonSpecimenRouting}/{species.Guid}");

        result!.Should().HaveCount(1);
        result.Single().SpecimenGuid.Should().Be(publicSpecimen.Guid);
        result.Select(s => s.SpecimenGuid).Should().NotContain(privateSpecimen.Guid);
    }

    /// <summary>
    /// Tests Get Specimen By Taxonomy – As Logged In User – Returns Own Private And Public Specimens.
    /// </summary>
    [Fact]
    public async Task GetSpecimenByTaxonomy_AsLoggedInUser_ReturnsOwnPrivateAndPublicSpecimens()
    {
        var owner    = await UserRepo.CreateUserAsync("owner@test.de");
        var species  = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var publicSpecimen  = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true,  createdByUserGuid: owner.Guid);
        var privateSpecimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: false, createdByUserGuid: owner.Guid);

        AuthenticateAs(owner);

        var result = await Client.GetFromJsonAsync<List<SpecimenWithTagsDto>>(
            $"{Routings.CommonSpecimenRouting}/{species.Guid}");

        result!.Should().HaveCount(2);
        result.Select(s => s.SpecimenGuid).Should().Contain(publicSpecimen.Guid);
        result.Select(s => s.SpecimenGuid).Should().Contain(privateSpecimen.Guid);
    }

    /// <summary>
    /// Tests Get Specimen By Taxonomy – As Logged In User – Does Not Return Other Users Private Specimens.
    /// </summary>
    [Fact]
    public async Task GetSpecimenByTaxonomy_AsLoggedInUser_DoesNotReturnOtherUsersPrivateSpecimens()
    {
        var owner   = await UserRepo.CreateUserAsync("owner@test.de");
        var viewer  = await UserRepo.CreateUserAsync("viewer@test.de");
        var species  = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var publicSpecimen  = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true,  createdByUserGuid: owner.Guid);
        var privateSpecimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: false, createdByUserGuid: owner.Guid);

        AuthenticateAs(viewer);

        var result = await Client.GetFromJsonAsync<List<SpecimenWithTagsDto>>(
            $"{Routings.CommonSpecimenRouting}/{species.Guid}");

        result!.Should().HaveCount(1);
        result.Single().SpecimenGuid.Should().Be(publicSpecimen.Guid);
        result.Select(s => s.SpecimenGuid).Should().NotContain(privateSpecimen.Guid);
    }

    /// <summary>
    /// Tests Get Specimen By Taxonomy – As Admin – Returns All Specimens Including Private.
    /// </summary>
    [Fact]
    public async Task GetSpecimenByTaxonomy_AsAdmin_ReturnsAllSpecimensIncludingPrivate()
    {
        var owner    = await UserRepo.CreateUserAsync("owner@test.de");
        var admin    = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var species  = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var publicSpecimen  = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true,  createdByUserGuid: owner.Guid);
        var privateSpecimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: false, createdByUserGuid: owner.Guid);

        AuthenticateAs(admin);

        var result = await Client.GetFromJsonAsync<List<SpecimenWithTagsDto>>(
            $"{Routings.CommonSpecimenRouting}/{species.Guid}");

        result!.Should().HaveCount(2);
        result.Select(s => s.SpecimenGuid).Should().Contain(publicSpecimen.Guid);
        result.Select(s => s.SpecimenGuid).Should().Contain(privateSpecimen.Guid);
    }

    #endregion

    #region Helpers

    private static readonly Guid AllSpecimensGuid = new("00000000-0000-0000-0000-000000000001");
    private static readonly Guid MyCollectionGuid = new("00000000-0000-0000-0000-000000000002");
    private static readonly Guid MyActiveGuid     = new("00000000-0000-0000-0000-000000000003");

    #endregion
}
