using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Model.Providers.Nominatim;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Common;

public class CommonSpecimenControllerTests : IntegrationTestBase
{
    public CommonSpecimenControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    #region CreateSpecimen

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

    #endregion

    #region CreateSpecimen – Tags

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

    [Fact]
    public async Task CreateSpecimen_InvalidSpeciesGuid_ReturnsNoTaxonomyWithThisGuid()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");

        SetupNominatimMock();
        AuthenticateAs(user);

        var request = BuildRequest(Guid.CreateVersion7());

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoTaxonomyWithThisGuid);
    }

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

    [Fact]
    public async Task CreateSpecimen_NotAuthenticated_ReturnsUnauthorized()
    {
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);

        var response = await Client.PostAsJsonAsync(Routings.CommonSpecimenRouting, BuildRequest(species.Guid));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GetPendingSpecimen

    [Fact]
    public async Task GetPendingSpecimen_AsAdmin_ReturnsAllPendingSpecimens()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var user1 = await UserRepo.CreateUserAsync("user1@test.de");
        var user2 = await UserRepo.CreateUserAsync("user2@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, SpecimenStatus.Pending, createdByUserGuid: user1.Guid);
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, SpecimenStatus.Pending, createdByUserGuid: user2.Guid);

        AuthenticateAs(admin);

        var response = await Client.GetAsync($"{Routings.CommonSpecimenRouting}/pending");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<GetAllSpecimenDto>>();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPendingSpecimen_AsUser_ReturnsOnlyOwnPendingSpecimens()
    {
        var user1 = await UserRepo.CreateUserAsync("user1@test.de");
        var user2 = await UserRepo.CreateUserAsync("user2@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var ownSpecimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, SpecimenStatus.Pending, createdByUserGuid: user1.Guid);
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, SpecimenStatus.Pending, createdByUserGuid: user2.Guid);

        AuthenticateAs(user1);

        var response = await Client.GetAsync($"{Routings.CommonSpecimenRouting}/pending");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<GetAllSpecimenDto>>();
        result.Should().HaveCount(1);
        result!.Single().Guid.Should().Be(ownSpecimen.Guid);
    }

    [Fact]
    public async Task GetPendingSpecimen_ActiveSpecimensNotReturned()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, SpecimenStatus.Active);

        AuthenticateAs(admin);

        var response = await Client.GetAsync($"{Routings.CommonSpecimenRouting}/pending");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<GetAllSpecimenDto>>();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPendingSpecimen_NotAuthenticated_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync($"{Routings.CommonSpecimenRouting}/pending");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GetDeletedSpecimen

    [Fact]
    public async Task GetDeletedSpecimen_ReturnsDeletedSpecimens()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var user1 = await UserRepo.CreateUserAsync("user1@test.de");
        var user2 = await UserRepo.CreateUserAsync("user2@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, SpecimenStatus.Deleted, createdByUserGuid: user1.Guid);
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, SpecimenStatus.Deleted, createdByUserGuid: user2.Guid);

        AuthenticateAs(admin);

        var response = await Client.GetAsync($"{Routings.CommonSpecimenRouting}/deleted");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<GetAllSpecimenDto>>();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetDeletedSpecimen_ActiveSpecimensNotReturned()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, SpecimenStatus.Active);

        AuthenticateAs(admin);

        var response = await Client.GetAsync($"{Routings.CommonSpecimenRouting}/deleted");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<GetAllSpecimenDto>>();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDeletedSpecimen_NotAuthenticated_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync($"{Routings.CommonSpecimenRouting}/deleted");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GetOwnSpecimen

    [Fact]
    public async Task GetOwnSpecimen_ReturnsDeletedSpecimens()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var user1 = await UserRepo.CreateUserAsync("user1@test.de");
        var user2 = await UserRepo.CreateUserAsync("user2@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, SpecimenStatus.Deleted, createdByUserGuid: user1.Guid);
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, SpecimenStatus.Deleted, createdByUserGuid: user2.Guid);

        AuthenticateAs(admin);

        var response = await Client.GetAsync($"{Routings.CommonSpecimenRouting}/own");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<GetAllSpecimenDto>>();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOwnSpecimen_ActiveSpecimensNotReturned()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, SpecimenStatus.Active);

        AuthenticateAs(admin);

        var response = await Client.GetAsync($"{Routings.CommonSpecimenRouting}/own");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<GetAllSpecimenDto>>();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOwnSpecimen_NotAuthenticated_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync($"{Routings.CommonSpecimenRouting}/own");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region UpdateSpecimen

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
            IsPublic = false,
            Status = SpecimenStatus.Active
        };

        var response = await Client.PatchAsJsonAsync($"{Routings.CommonSpecimenRouting}/{specimen.Guid}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await GetSpecimenFromDbAsync(specimen.Guid);
        updated!.Name.Should().Be("Updated Name");
        updated.LatinName.Should().Be("Updated latinname");
        updated.Comment.Should().Be("New comment");
        updated.IsPublic.Should().BeFalse();
    }

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
            IsPublic = specimen.IsPublic,
            Status = specimen.Status
        };

        var response = await Client.PatchAsJsonAsync($"{Routings.CommonSpecimenRouting}/{specimen.Guid}", request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoUpdatedData);
    }

    [Fact]
    public async Task UpdateSpecimen_InvalidSpecimenGuid_ReturnsNoSpecimenWithThisGuid()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        AuthenticateAs(user);

        var request = new UpdateSpecimenRequestDto
        {
            Name = "X",
            LatinName = "X",
            DateOfDiscovery = DateTime.UtcNow,
            Comment = null,
            IsPublic = true,
            Status = SpecimenStatus.Active
        };

        var response = await Client.PatchAsJsonAsync($"{Routings.CommonSpecimenRouting}/{Guid.CreateVersion7()}", request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoSpecimenWithThisGuid);
    }

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
            IsPublic = true,
            Status = SpecimenStatus.Active
        };

        var response = await Client.PatchAsJsonAsync($"{Routings.CommonSpecimenRouting}/{specimen.Guid}", request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

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
            IsPublic = specimen.IsPublic,
            Status = specimen.Status
        };

        var response = await Client.PatchAsJsonAsync($"{Routings.CommonSpecimenRouting}/{specimen.Guid}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateSpecimen_NotAuthenticated_ReturnsUnauthorized()
    {
        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonSpecimenRouting}/{Guid.CreateVersion7()}",
            new UpdateSpecimenRequestDto
            {
                Name = "X", LatinName = "X", DateOfDiscovery = DateTime.UtcNow,
                Comment = null, IsPublic = true, Status = SpecimenStatus.Active
            });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region UpdateSpecimenImage

    [Fact]
    public async Task UpdateSpecimenImage_WithNewImage_ReplacesExistingImage()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenWithImageAsync(
            species.Guid, location.Guid, imageBase64: "oldImage", createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonSpecimenRouting}/image/{specimen.Guid}",
            new UpdateSpecimenImageRequestDto { ImageBase64 = "newImage" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var images = await GetImagesForSpecimenFromDbAsync(specimen.Guid);
        images.Should().HaveCount(1);
        images.Single().ImageBase64.Should().Be("newImage");
    }

    [Fact]
    public async Task UpdateSpecimenImage_WithNullImage_DeletesExistingImage()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenWithImageAsync(
            species.Guid, location.Guid, imageBase64: "someImage", createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonSpecimenRouting}/image/{specimen.Guid}",
            new UpdateSpecimenImageRequestDto { ImageBase64 = null });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var images = await GetImagesForSpecimenFromDbAsync(specimen.Guid);
        images.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateSpecimenImage_SpecimenHasNoImage_AddsNewImage()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(
            species.Guid, location.Guid, createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonSpecimenRouting}/image/{specimen.Guid}",
            new UpdateSpecimenImageRequestDto { ImageBase64 = "brandNew" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var images = await GetImagesForSpecimenFromDbAsync(specimen.Guid);
        images.Should().HaveCount(1);
        images.Single().ImageBase64.Should().Be("brandNew");
    }

    [Fact]
    public async Task UpdateSpecimenImage_SameImageData_ReturnsNoUpdatedData()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenWithImageAsync(
            species.Guid, location.Guid, imageBase64: "same", createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonSpecimenRouting}/image/{specimen.Guid}",
            new UpdateSpecimenImageRequestDto { ImageBase64 = "same" });

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoUpdatedData);
    }

    [Fact]
    public async Task UpdateSpecimenImage_InvalidSpecimenGuid_ReturnsNoSpecimenWithThisGuid()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        AuthenticateAs(user);

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonSpecimenRouting}/image/{Guid.CreateVersion7()}",
            new UpdateSpecimenImageRequestDto { ImageBase64 = "data" });

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoSpecimenWithThisGuid);
    }

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

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonSpecimenRouting}/image/{specimen.Guid}",
            new UpdateSpecimenImageRequestDto { ImageBase64 = "hacked" });

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    [Fact]
    public async Task UpdateSpecimenImage_NotAuthenticated_ReturnsUnauthorized()
    {
        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonSpecimenRouting}/image/{Guid.CreateVersion7()}",
            new UpdateSpecimenImageRequestDto { ImageBase64 = "data" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region UpdateSpecimenTags

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
            $"{Routings.CommonSpecimenRouting}/tags/{specimen.Guid}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var relations = await GetTagRelationsForSpecimenFromDbAsync(specimen.Guid);
        relations.Should().HaveCount(1);
        relations.Single().TagGuid.Should().Be(tag2.Guid);
    }

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
            $"{Routings.CommonSpecimenRouting}/tags/{specimen.Guid}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var relations = await GetTagRelationsForSpecimenFromDbAsync(specimen.Guid);
        relations.Should().HaveCount(2);
    }

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
            $"{Routings.CommonSpecimenRouting}/tags/{specimen.Guid}", request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoUpdatedData);
    }

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
            $"{Routings.CommonSpecimenRouting}/tags/{specimen.Guid}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var relations = await GetTagRelationsForSpecimenFromDbAsync(specimen.Guid);
        relations.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateSpecimenTags_InvalidSpecimenGuid_ReturnsNoSpecimenWithThisGuid()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        AuthenticateAs(user);

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonSpecimenRouting}/tags/{Guid.CreateVersion7()}",
            new UpdateSpecimenTagsRequestDto { SpecimenTags = [] });

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoSpecimenWithThisGuid);
    }

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
            $"{Routings.CommonSpecimenRouting}/tags/{specimen.Guid}", request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    [Fact]
    public async Task UpdateSpecimenTags_NotAuthenticated_ReturnsUnauthorized()
    {
        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonSpecimenRouting}/tags/{Guid.CreateVersion7()}",
            new UpdateSpecimenTagsRequestDto { SpecimenTags = [] });

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
        Guid taxonomySpeciesGuid,
        List<Guid>? tags = null,
        List<string>? tagsToCreate = null,
        string? base64Image = null) => new()
    {
        Name = "Test Specimen",
        LatinName = "Testus specienus",
        DateOfDiscovery = DateTime.UtcNow,
        Location = new GeoDto { Latitude = 48.137, Longitude = 11.575 },
        TaxonomySpeciesGuid = taxonomySpeciesGuid,
        Tags = tags ?? [],
        TagsToCreate = tagsToCreate ?? [],
        Base64Image = base64Image ?? string.Empty
    };

    #endregion
}
