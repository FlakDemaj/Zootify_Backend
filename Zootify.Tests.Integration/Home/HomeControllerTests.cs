using Zootify_Backend.DTOs.Common.Response;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Home;

/// <summary>
/// Tests for Home Controller.
/// </summary>
public class HomeControllerTests : IntegrationTestBase
{
    public HomeControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    private async Task<(TaxonomyNodeEntity species, GeographicLocationEntity location)> ArrangeAsync()
    {
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        return (species, location);
    }

    /// <summary>
    /// Builds a Country → City location chain and returns the city node (leaf).
    /// </summary>
    private async Task<GeographicLocationEntity> CreateCityLocationAsync(string cityName)
    {
        var country = await SpecimenRepo.CreateGeographicLocationAsync("Deutschland", GeographicLevel.Country);
        return await SpecimenRepo.CreateGeographicLocationAsync(cityName, GeographicLevel.City, parentGuid: country.Guid);
    }

    #region Visibility

    /// <summary>
    /// Tests Get Overview – Anonymous – Returns Only Public Specimens.
    /// </summary>
    [Fact]
    public async Task GetOverview_Anonymous_ReturnsOnlyPublicSpecimens()
    {
        var (species, location) = await ArrangeAsync();
        var publicSpecimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true);
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: false);

        var response = await Client.GetAsync(Routings.HomeAnimalsRouting);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetPublicSpecimenOverviewResponseDto>();
        result!.TotalCount.Should().Be(1);
        result.Specimens.Should().ContainSingle();
        result.Specimens.Single().SpecimenGuid.Should().Be(publicSpecimen.Guid);
        result.Specimens.Single().Name.Should().Be(publicSpecimen.Name);
    }

    /// <summary>
    /// Tests Get Overview – Moderator – Sees All Including Others Non Public.
    /// </summary>
    [Fact]
    public async Task GetOverview_Moderator_SeesAllIncludingOthersNonPublic()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var moderator = await UserRepo.CreateUserAsync("mod@test.de", role: UserRole.Moderator);
        var (species, location) = await ArrangeAsync();
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true);
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: false, createdByUserGuid: owner.Guid);

        AuthenticateAs(moderator);

        var result = await Client.GetFromJsonAsync<GetPublicSpecimenOverviewResponseDto>(Routings.HomeAnimalsRouting);

        result!.TotalCount.Should().Be(2);
        result.Specimens.Should().HaveCount(2);
    }

    /// <summary>
    /// Tests Get Overview – Admin – Sees All Including Others Non Public.
    /// </summary>
    [Fact]
    public async Task GetOverview_Admin_SeesAllIncludingOthersNonPublic()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var (species, location) = await ArrangeAsync();
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true);
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: false, createdByUserGuid: owner.Guid);

        AuthenticateAs(admin);

        var result = await Client.GetFromJsonAsync<GetPublicSpecimenOverviewResponseDto>(Routings.HomeAnimalsRouting);

        result!.TotalCount.Should().Be(2);
        result.Specimens.Should().HaveCount(2);
    }

    /// <summary>
    /// Tests Get Overview – Regular User – Sees Public And Own Non Public – But Not Others.
    /// </summary>
    [Fact]
    public async Task GetOverview_RegularUser_SeesPublicAndOwnNonPublic_ButNotOthers()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var user = await UserRepo.CreateUserAsync("user@test.de", role: UserRole.User);
        var (species, location) = await ArrangeAsync();
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true);
        var ownNonPublic = await SpecimenRepo.CreateSpecimenAsync(
            species.Guid, location.Guid, isPublic: false, createdByUserGuid: user.Guid);
        await SpecimenRepo.CreateSpecimenAsync(
            species.Guid, location.Guid, isPublic: false, createdByUserGuid: owner.Guid);

        AuthenticateAs(user);

        var result = await Client.GetFromJsonAsync<GetPublicSpecimenOverviewResponseDto>(Routings.HomeAnimalsRouting);

        // Public + own non-public are visible; the other user's non-public is hidden.
        result!.TotalCount.Should().Be(2);
        result.Specimens.Should().Contain(s => s.SpecimenGuid == ownNonPublic.Guid);
    }

    #endregion

    #region Paging & Sorting

    /// <summary>
    /// Tests Get Overview – Paging – Returns At Most20 Per Page.
    /// </summary>
    [Fact]
    public async Task GetOverview_Paging_ReturnsAtMost20PerPage()
    {
        var (species, location) = await ArrangeAsync();
        for (var i = 0; i < 25; i++)
        {
            await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true, name: $"Animal {i}");
        }

        var result = await Client.GetFromJsonAsync<GetPublicSpecimenOverviewResponseDto>(Routings.HomeAnimalsRouting);

        result!.TotalCount.Should().Be(25);
        result.Specimens.Should().HaveCount(20);
    }

    /// <summary>
    /// Tests Get Overview – Default Sort – Returns Newest First.
    /// </summary>
    [Fact]
    public async Task GetOverview_DefaultSort_ReturnsNewestFirst()
    {
        var (species, location) = await ArrangeAsync();

        var oldest = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true, name: "Oldest");
        var middle = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true, name: "Middle");
        var newest = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true, name: "Newest");

        await OverrideCreationDateAsync(oldest, DateTime.UtcNow.AddDays(-3));
        await OverrideCreationDateAsync(middle, DateTime.UtcNow.AddDays(-2));
        await OverrideCreationDateAsync(newest, DateTime.UtcNow.AddDays(-1));

        var result = await Client.GetFromJsonAsync<GetPublicSpecimenOverviewResponseDto>(Routings.HomeAnimalsRouting);

        result!.Specimens.Select(s => s.Name).Should().ContainInOrder("Newest", "Middle", "Oldest");
    }

    #endregion

    #region FilterByCity

    /// <summary>
    /// Tests Get Overview – Filter By Ort – Returns Only Matching City.
    /// </summary>
    [Fact]
    public async Task GetOverview_FilterByCity_ReturnsOnlyMatchingCity()
    {
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var siegen = await CreateCityLocationAsync("Siegen");
        var koeln = await CreateCityLocationAsync("Köln");

        var siegenSpecimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, siegen.Guid, isPublic: true, name: "Siegen Animal");
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, koeln.Guid, isPublic: true, name: "Köln Animal");

        var result = await Client.GetFromJsonAsync<GetPublicSpecimenOverviewResponseDto>(
            $"{Routings.HomeAnimalsRouting}?filterByCity=Siegen");

        result!.TotalCount.Should().Be(1);
        result.Specimens.Should().ContainSingle(s => s.SpecimenGuid == siegenSpecimen.Guid);
        result.Specimens.Single().LocationPath
            .Should().Contain(n => n.Level == GeographicLevel.City && n.Name == "Siegen");
    }

    /// <summary>
    /// Tests Get Overview – Filter By Ort – Is Case Insensitive.
    /// </summary>
    [Fact]
    public async Task GetOverview_FilterByCity_IsCaseInsensitive()
    {
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var siegen = await CreateCityLocationAsync("Siegen");
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, siegen.Guid, isPublic: true, name: "Siegen Animal");

        var result = await Client.GetFromJsonAsync<GetPublicSpecimenOverviewResponseDto>(
            $"{Routings.HomeAnimalsRouting}?filterByCity=siegen");

        result!.TotalCount.Should().Be(1);
        result.Specimens.Single().LocationPath
            .Should().Contain(n => n.Level == GeographicLevel.City && n.Name == "Siegen");
    }

    #endregion
}
