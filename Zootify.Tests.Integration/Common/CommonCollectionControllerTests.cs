using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Common;

/// <summary>
/// Tests for Common Collection Controller.
/// </summary>
public class CommonCollectionControllerTests : IntegrationTestBase
{
    public CommonCollectionControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    #region CreateCollection

    /// <summary>
    /// Tests Create Collection – Valid Request – Returns Created Guid.
    /// </summary>
    [Fact]
    public async Task CreateCollection_ValidRequest_ReturnsCreatedGuid()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var request = new CreateCollectionRequestDto
        {
            Name = "My Collection",
            Description = "A test collection",
            IsPrivate = false
        };

        var response = await Client.PostAsJsonAsync(Routings.CommonCollectionRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CreatedObjectResponseDto>();
        result.Should().NotBeNull();

        var collection = await GetCollectionFromDbAsync(result!.CreatedObjectGuid);
        collection.Should().NotBeNull();
        collection!.Name.Should().Be("My Collection");
        collection.CreatedByUserGuid.Should().Be(user.Guid);
    }

    /// <summary>
    /// Tests Create Collection – Not Authenticated – Returns Unauthorized.
    /// </summary>
    [Fact]
    public async Task CreateCollection_NotAuthenticated_ReturnsUnauthorized()
    {
        var request = new CreateCollectionRequestDto { Name = "X", IsPrivate = false };
        var response = await Client.PostAsJsonAsync(Routings.CommonCollectionRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GetAllCollections

    /// <summary>
    /// Tests Get All Collections – Authenticated – Contains Six Static Collections.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_Authenticated_ContainsSixStaticCollections()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var response = await Client.GetAsync(Routings.CommonCollectionRouting);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        result!.TotalCount.Should().Be(6);
        result.Collections.Should().Contain(c => c.Name == "Alle Exemplare");
        result.Collections.Should().Contain(c => c.Name == "Meine Sammlung");
        result.Collections.Should().Contain(c => c.Name == "Meine Aktiven");
        result.Collections.Should().Contain(c => c.Name == "Meine Ausstehenden");
        result.Collections.Should().Contain(c => c.Name == "Meine Gelöschten");
        result.Collections.Should().Contain(c => c.Name == "Meine Abgelehnten");
    }

    /// <summary>
    /// Tests Get All Collections – Authenticated – Returns Static Plus Db Collections.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_Authenticated_ReturnsStaticPlusDbCollections()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        await CollectionRepo.CreateCollectionAsync(name: "Collection A", isPrivate: false);
        await CollectionRepo.CreateCollectionAsync(name: "Collection B", isPrivate: false);

        AuthenticateAs(user);

        var response = await Client.GetAsync(Routings.CommonCollectionRouting);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        result!.TotalCount.Should().Be(8);
        result.Collections.Should().HaveCount(8);
    }

    /// <summary>
    /// Tests Get All Collections – Dynamic Collection – Reports Actual Specimen Count.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_DynamicCollection_ReportsActualSpecimenCount()
    {
        var user = await UserRepo.CreateUserAsync("count@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("Felidae", TaxonomyRank.Family);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var collection = await CollectionRepo.CreateCollectionAsync(
            name: "Counted", isPrivate: false, createdByUserGuid: user.Guid);

        var first = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid,
            createdByUserGuid: user.Guid);
        var second = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid,
            createdByUserGuid: user.Guid);
        // An inactive specimen in the same collection must not be counted.
        var inactive = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid,
            SpecimenStatus.Deleted, createdByUserGuid: user.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, first.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, second.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, inactive.Guid);

        AuthenticateAs(user);

        var response = await Client.GetAsync(Routings.CommonCollectionRouting);

        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        result!.Collections.Single(c => c.Name == "Counted").SpecimenCount.Should().Be(2);
    }

    /// <summary>
    /// Tests Get All Collections – Dynamic Collection – Regular User Does Not Count Others' Private Specimens.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_DynamicCollection_User_OnlyCountsActiveVisibleSpecimens()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var viewer = await UserRepo.CreateUserAsync("viewer@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("Felidae", TaxonomyRank.Family);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var collection = await CollectionRepo.CreateCollectionAsync(
            name: "Mixed", isPrivate: false, createdByUserGuid: owner.Guid);

        var publicActive = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid,
            isPublic: true, createdByUserGuid: owner.Guid);
        var ownPrivateActive = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid,
            isPublic: false, createdByUserGuid: viewer.Guid);
        var othersPrivateActive = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid,
            isPublic: false, createdByUserGuid: owner.Guid);
        var publicInactive = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid,
            SpecimenStatus.Pending, isPublic: true, createdByUserGuid: owner.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, publicActive.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, ownPrivateActive.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, othersPrivateActive.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, publicInactive.Guid);

        AuthenticateAs(viewer);

        var response = await Client.GetAsync(Routings.CommonCollectionRouting);

        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        // Public active + own private active are counted; others' private and the inactive one are not.
        result!.Collections.Single(c => c.Name == "Mixed").SpecimenCount.Should().Be(2);
    }

    /// <summary>
    /// Tests Get All Collections – Dynamic Collection – Admin Counts All Active Specimens Regardless Of Visibility.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_DynamicCollection_Admin_CountsAllActiveSpecimens()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("Felidae", TaxonomyRank.Family);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var collection = await CollectionRepo.CreateCollectionAsync(
            name: "Mixed", isPrivate: false, createdByUserGuid: owner.Guid);

        var publicActive = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid,
            isPublic: true, createdByUserGuid: owner.Guid);
        var othersPrivateActive = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid,
            isPublic: false, createdByUserGuid: owner.Guid);
        var inactive = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid,
            SpecimenStatus.Deleted, isPublic: true, createdByUserGuid: owner.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, publicActive.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, othersPrivateActive.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, inactive.Guid);

        AuthenticateAs(admin);

        var response = await Client.GetAsync(Routings.CommonCollectionRouting);

        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        // Admin counts every active specimen (public and private), but never the inactive one.
        result!.Collections.Single(c => c.Name == "Mixed").SpecimenCount.Should().Be(2);
    }

    /// <summary>
    /// Tests Get All Collections – Private Collections Not Returned For Other Users.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_PrivateCollectionsNotReturnedForOtherUsers()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var viewer = await UserRepo.CreateUserAsync("viewer@test.de");
        await CollectionRepo.CreateCollectionAsync(name: "Public", isPrivate: false, createdByUserGuid: owner.Guid);
        await CollectionRepo.CreateCollectionAsync(name: "Private", isPrivate: true, createdByUserGuid: owner.Guid);

        AuthenticateAs(viewer);

        var response = await Client.GetAsync(Routings.CommonCollectionRouting);

        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        result!.Collections.Should().Contain(c => c.Name == "Public");
        result.Collections.Should().NotContain(c => c.Name == "Private");
    }

    /// <summary>
    /// Tests Get All Collections – Own Private Collection Is Visible.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_OwnPrivateCollectionIsVisible()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        await CollectionRepo.CreateCollectionAsync(name: "My Secret", isPrivate: true, createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var response = await Client.GetAsync(Routings.CommonCollectionRouting);

        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        result!.Collections.Should().Contain(c => c.Name == "My Secret");
    }

    /// <summary>
    /// Tests Get All Collections – Deleted Collections Not Returned.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_DeletedCollectionsNotReturned()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        await CollectionRepo.CreateCollectionAsync(name: "ActiveColl", isPrivate: false);
        await CollectionRepo.CreateCollectionAsync(name: "DeletedColl", isPrivate: false,
            deletedByUserGuid: user.Guid);

        AuthenticateAs(user);

        var response = await Client.GetAsync(Routings.CommonCollectionRouting);

        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        result!.Collections.Should().Contain(c => c.Name == "ActiveColl");
        result.Collections.Should().NotContain(c => c.Name == "DeletedColl");
    }

    /// <summary>
    /// Tests Get All Collections – Static Collections – Reflect Actual Specimen Counts.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_StaticCollections_ReflectActualSpecimenCounts()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();

        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, SpecimenStatus.Active,
            createdByUserGuid: user.Guid);
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, SpecimenStatus.Active,
            createdByUserGuid: user.Guid);
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, SpecimenStatus.Pending,
            createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var response = await Client.GetAsync(Routings.CommonCollectionRouting);
        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();

        result!.Collections.Single(c => c.Name == "Alle Exemplare").SpecimenCount.Should().Be(3);
        result.Collections.Single(c => c.Name == "Meine Sammlung").SpecimenCount.Should().Be(3);
        result.Collections.Single(c => c.Name == "Meine Aktiven").SpecimenCount.Should().Be(2);
        result.Collections.Single(c => c.Name == "Meine Ausstehenden").SpecimenCount.Should().Be(1);
        result.Collections.Single(c => c.Name == "Meine Gelöschten").SpecimenCount.Should().Be(0);
        result.Collections.Single(c => c.Name == "Meine Abgelehnten").SpecimenCount.Should().Be(0);
    }

    /// <summary>
    /// Tests Get All Collections – Static Collections – Only Own Specimen Counts Shown.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_StaticCollections_OnlyOwnSpecimenCountsShown()
    {
        var user1 = await UserRepo.CreateUserAsync("user1@test.de");
        var user2 = await UserRepo.CreateUserAsync("user2@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();

        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, SpecimenStatus.Active,
            createdByUserGuid: user1.Guid);
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, SpecimenStatus.Deleted,
            createdByUserGuid: user1.Guid);
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, SpecimenStatus.Active,
            createdByUserGuid: user2.Guid);
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, SpecimenStatus.Active,
            createdByUserGuid: user2.Guid);

        AuthenticateAs(user1);

        var response = await Client.GetAsync(Routings.CommonCollectionRouting);
        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();

        result!.Collections.Single(c => c.Name == "Alle Exemplare").SpecimenCount.Should().Be(4);
        result.Collections.Single(c => c.Name == "Meine Aktiven").SpecimenCount.Should().Be(1);
        result.Collections.Single(c => c.Name == "Meine Gelöschten").SpecimenCount.Should().Be(1);
        result.Collections.Single(c => c.Name == "Meine Sammlung").SpecimenCount.Should().Be(2);
        result.Collections.Single(c => c.Name == "Meine Abgelehnten").SpecimenCount.Should().Be(0);
    }

    /// <summary>
    /// Tests Get All Collections – Static Collections – Admin Only Sees Own Specimen Counts.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_StaticCollections_AdminOnlySeesOwnSpecimenCounts()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var otherUser = await UserRepo.CreateUserAsync("other@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();

        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, SpecimenStatus.Active,
            createdByUserGuid: admin.Guid);
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, SpecimenStatus.Active,
            createdByUserGuid: otherUser.Guid);
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, SpecimenStatus.Active,
            createdByUserGuid: otherUser.Guid);

        AuthenticateAs(admin);

        var response = await Client.GetAsync(Routings.CommonCollectionRouting);
        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();

        result!.Collections.Single(c => c.Name == "Meine Aktiven").SpecimenCount.Should().Be(1);
        result.Collections.Single(c => c.Name == "Meine Sammlung").SpecimenCount.Should().Be(1);
    }

    /// <summary>
    /// Tests Get All Collections – Static Collections – Private Specimens Are Counted.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_StaticCollections_PrivateSpecimensAreCounted()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();

        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, SpecimenStatus.Active,
            createdByUserGuid: user.Guid, isPublic: false);
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, SpecimenStatus.Active,
            createdByUserGuid: user.Guid, isPublic: true);

        AuthenticateAs(user);

        var response = await Client.GetAsync(Routings.CommonCollectionRouting);
        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();

        result!.Collections.Single(c => c.Name == "Meine Aktiven").SpecimenCount.Should().Be(2);
    }

    #endregion

    #region GetAllCollections – FilterByName

    /// <summary>
    /// Tests Get All Collections – Filter By Name – Returns Dynamic Matches Only.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_FilterByName_ReturnsDynamicMatchesOnly()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        await CollectionRepo.CreateCollectionAsync(name: "Birds of Europe", isPrivate: false);
        await CollectionRepo.CreateCollectionAsync(name: "Mammals", isPrivate: false);

        AuthenticateAs(user);

        var response = await Client.GetAsync($"{Routings.CommonCollectionRouting}?filterByName=Birds");

        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        result!.Collections.Should().Contain(c => c.Name == "Birds of Europe");
        result.Collections.Should().NotContain(c => c.Name == "Mammals");
    }

    /// <summary>
    /// Tests Get All Collections – Fuzzy Filter – Finds Collection Despite Typo.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_FuzzyFilter_FindsCollectionDespiteTypo()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        await CollectionRepo.CreateCollectionAsync(name: "Birds of Europe", isPrivate: false);
        await CollectionRepo.CreateCollectionAsync(name: "Mammals", isPrivate: false);
        AuthenticateAs(user);

        // "Birts of Europe" is a typo of "Birds of Europe" → fuzzy matching still finds it.
        var response = await Client.GetAsync(
            $"{Routings.CommonCollectionRouting}?filterByName={Uri.EscapeDataString("Birts of Europe")}");

        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        result!.Collections.Should().Contain(c => c.Name == "Birds of Europe");
        result.Collections.Should().NotContain(c => c.Name == "Mammals");
    }

    /// <summary>
    /// Tests Get All Collections – Filter By Name – Also Filters Static Collections.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_FilterByName_AlsoFiltersStaticCollections()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var response = await Client.GetAsync($"{Routings.CommonCollectionRouting}?filterByName=Meine");

        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        result!.Collections.Should().Contain(c => c.Name == "Meine Sammlung");
        result.Collections.Should().Contain(c => c.Name == "Meine Aktiven");
        result.Collections.Should().Contain(c => c.Name == "Meine Ausstehenden");
        result.Collections.Should().Contain(c => c.Name == "Meine Gelöschten");
        result.Collections.Should().Contain(c => c.Name == "Meine Abgelehnten");
        result.Collections.Should().NotContain(c => c.Name == "Alle Exemplare");
    }

    /// <summary>
    /// Tests Get All Collections – Filter By Name – Case Insensitive On Static Collections.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_FilterByName_CaseInsensitiveOnStaticCollections()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var response = await Client.GetAsync($"{Routings.CommonCollectionRouting}?filterByName=alle");

        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        result!.Collections.Should().Contain(c => c.Name == "Alle Exemplare");
    }

    /// <summary>
    /// Tests Get All Collections – Filter By Name – No Match – Returns Empty Collections.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_FilterByName_NoMatch_ReturnsEmptyCollections()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        await CollectionRepo.CreateCollectionAsync(name: "Birds", isPrivate: false);
        AuthenticateAs(user);

        var response = await Client.GetAsync($"{Routings.CommonCollectionRouting}?filterByName=zzznomatch");

        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        result!.TotalCount.Should().Be(0);
        result.Collections.Should().BeEmpty();
    }

    #endregion

    #region GetAllCollections – Paging

    /// <summary>
    /// Tests Get All Collections – Paging – Returns Correct Page.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_Paging_ReturnsCorrectPage()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        for (var i = 0; i < 5; i++)
        {
            await CollectionRepo.CreateCollectionAsync(name: $"Col {i}", isPrivate: false);
        }

        AuthenticateAs(user);

        var response = await Client.GetAsync(
            $"{Routings.CommonCollectionRouting}?page=1&pageSize=5");

        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        result!.TotalCount.Should().Be(11); // 6 statics + 5 dynamic
        result.Collections.Should().HaveCount(5);
    }

    /// <summary>
    /// Tests Get All Collections – Paging – Second Page Returns Remaining Items.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_Paging_SecondPageReturnsRemainingItems()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        for (var i = 0; i < 5; i++)
        {
            await CollectionRepo.CreateCollectionAsync(name: $"Col {i}", isPrivate: false);
        }

        AuthenticateAs(user);

        var response = await Client.GetAsync(
            $"{Routings.CommonCollectionRouting}?page=2&pageSize=8");

        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        result!.TotalCount.Should().Be(11); // 6 statics + 5 dynamic
        result.Collections.Should().HaveCount(3); // 11 - 8 = 3 on page 2
    }

    /// <summary>
    /// Tests Get All Collections – Paging – Total Count Matches With Filter.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_Paging_TotalCountMatchesWithFilter()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        await CollectionRepo.CreateCollectionAsync(name: "Birds A", isPrivate: false);
        await CollectionRepo.CreateCollectionAsync(name: "Birds B", isPrivate: false);
        await CollectionRepo.CreateCollectionAsync(name: "Mammals", isPrivate: false);
        AuthenticateAs(user);

        var response = await Client.GetAsync(
            $"{Routings.CommonCollectionRouting}?filterByName=Birds&page=1&pageSize=1");

        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        result!.TotalCount.Should().Be(2);
        result.Collections.Should().HaveCount(1);
    }

    #endregion

    #region GetAllCollections – SortBy

    /// <summary>
    /// Tests Get All Collections – Sort By Name – Asc – Returns Dynamic Collections Sorted Ascending.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_SortByName_Asc_ReturnsDynamicCollectionsSortedAscending()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        await CollectionRepo.CreateCollectionAsync(name: "Zebra Collection", isPrivate: false);
        await CollectionRepo.CreateCollectionAsync(name: "Aardvark Collection", isPrivate: false);
        await CollectionRepo.CreateCollectionAsync(name: "Mammals", isPrivate: false);

        AuthenticateAs(user);

        var response = await Client.GetAsync(
            $"{Routings.CommonCollectionRouting}?sortBy=name&sortDirection=ASC");

        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        var dynamicNames = result!.Collections
            .Skip(6)
            .Select(c => c.Name)
            .ToList();

        dynamicNames.Should().BeInAscendingOrder();
    }

    /// <summary>
    /// Tests Get All Collections – Sort By Name – Desc – Returns Dynamic Collections Sorted Descending.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_SortByName_Desc_ReturnsDynamicCollectionsSortedDescending()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        await CollectionRepo.CreateCollectionAsync(name: "Zebra Collection", isPrivate: false);
        await CollectionRepo.CreateCollectionAsync(name: "Aardvark Collection", isPrivate: false);
        await CollectionRepo.CreateCollectionAsync(name: "Mammals", isPrivate: false);

        AuthenticateAs(user);

        var response = await Client.GetAsync(
            $"{Routings.CommonCollectionRouting}?sortBy=name&sortDirection=DESC");

        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        var dynamicNames = result!.Collections
            .Skip(6)
            .Select(c => c.Name)
            .ToList();

        dynamicNames.Should().BeInDescendingOrder();
    }

    /// <summary>
    /// Tests Get All Collections – Sort By Specimen Count – Desc – Sorts Entire List Descending.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_SortBySpecimenCount_Desc_SortsEntireListDescending()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();

        var colA = await CollectionRepo.CreateCollectionAsync(name: "Col A", isPrivate: false,
            createdByUserGuid: user.Guid);
        var colB = await CollectionRepo.CreateCollectionAsync(name: "Col B", isPrivate: false,
            createdByUserGuid: user.Guid);

        var sp1 = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid,
            createdByUserGuid: user.Guid);
        var sp2 = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid,
            createdByUserGuid: user.Guid);
        var sp3 = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid,
            createdByUserGuid: user.Guid);

        await CollectionRepo.CreateSpecimenCollectionAsync(colA.Guid, sp1.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(colA.Guid, sp2.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(colA.Guid, sp3.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(colB.Guid, sp1.Guid);

        AuthenticateAs(user);

        var response = await Client.GetAsync(
            $"{Routings.CommonCollectionRouting}?sortBy=specimenCount&sortDirection=DESC");

        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        var counts = result!.Collections.Select(c => c.SpecimenCount).ToList();

        counts.Should().BeInDescendingOrder();
    }

    /// <summary>
    /// Tests Get All Collections – Sort By Specimen Count – Asc – Sorts Entire List Ascending.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_SortBySpecimenCount_Asc_SortsEntireListAscending()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();

        var colA = await CollectionRepo.CreateCollectionAsync(name: "Col A", isPrivate: false,
            createdByUserGuid: user.Guid);
        var colB = await CollectionRepo.CreateCollectionAsync(name: "Col B", isPrivate: false,
            createdByUserGuid: user.Guid);

        var sp1 = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid,
            createdByUserGuid: user.Guid);
        var sp2 = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid,
            createdByUserGuid: user.Guid);

        await CollectionRepo.CreateSpecimenCollectionAsync(colA.Guid, sp1.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(colA.Guid, sp2.Guid);

        AuthenticateAs(user);

        var response = await Client.GetAsync(
            $"{Routings.CommonCollectionRouting}?sortBy=specimenCount&sortDirection=ASC");

        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        var counts = result!.Collections.Select(c => c.SpecimenCount).ToList();

        counts.Should().BeInAscendingOrder();
    }

    /// <summary>
    /// Tests Get All Collections – Sort By Created At – Desc – Returns Dynamic Collections Sorted Descending.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_SortByCreatedAt_Desc_ReturnsDynamicCollectionsSortedDescending()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        await CollectionRepo.CreateCollectionAsync(name: "First", isPrivate: false);
        await CollectionRepo.CreateCollectionAsync(name: "Second", isPrivate: false);
        await CollectionRepo.CreateCollectionAsync(name: "Third", isPrivate: false);

        AuthenticateAs(user);

        var response = await Client.GetAsync(
            $"{Routings.CommonCollectionRouting}?sortBy=createdAt&sortDirection=DESC");

        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        var dynamicDates = result!.Collections
            .Skip(6)
            .Where(c => c.CreatedAt.HasValue)
            .Select(c => c.CreatedAt!.Value)
            .ToList();

        dynamicDates.Should().BeInDescendingOrder();
    }

    /// <summary>
    /// Tests Get All Collections – Sort By Name – Static And Dynamic Collections Are Interleaved.
    /// A dynamic collection that sorts before the first static collection must appear first, proving
    /// the static collections are no longer pinned to the top but ordered together with the dynamic ones.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_SortByName_Asc_InterleavesStaticAndDynamicCollections()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        // "Aaa" sorts before the first static ("Alle Exemplare"); "Zzz" sorts after every static.
        await CollectionRepo.CreateCollectionAsync(name: "Aaa Collection", isPrivate: false);
        await CollectionRepo.CreateCollectionAsync(name: "Zzz Collection", isPrivate: false);
        await CollectionRepo.CreateCollectionAsync(name: "Mmm Collection", isPrivate: false);

        AuthenticateAs(user);

        var response = await Client.GetAsync(
            $"{Routings.CommonCollectionRouting}?sortBy=name&sortDirection=ASC");

        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        var names = result!.Collections.Select(c => c.Name).ToList();

        result.TotalCount.Should().Be(9); // 6 static + 3 dynamic
        names.Should().BeInAscendingOrder();
        names.First().Should().Be("Aaa Collection");
        names.Last().Should().Be("Zzz Collection");
    }

    /// <summary>
    /// Tests Get All Collections – Sort By Created At – Static Collections Are Always Last.
    /// Virtual static collections have no creation date, so they must be ordered after every dynamic
    /// collection regardless of the sort direction.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_SortByCreatedAt_StaticCollectionsAlwaysLast()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        await CollectionRepo.CreateCollectionAsync(name: "First", isPrivate: false);
        await CollectionRepo.CreateCollectionAsync(name: "Second", isPrivate: false);

        AuthenticateAs(user);

        var response = await Client.GetAsync(
            $"{Routings.CommonCollectionRouting}?sortBy=createdAt&sortDirection=ASC");

        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        var collections = result!.Collections;

        // The two dynamic collections (with a creation date) come first, the six statics (null) last.
        collections.Take(2).Should().OnlyContain(c => c.CreatedAt.HasValue);
        collections.Skip(2).Should().OnlyContain(c => c.CreatedAt == null);
        collections.Skip(2).Should().HaveCount(6);
    }

    #endregion

    #region UpdateCollection

    /// <summary>
    /// Tests Update Collection – All Data – Updates Collection.
    /// </summary>
    [Fact]
    public async Task UpdateCollection_AllData_UpdatesCollection()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(
            name: "Old Name",
            description: "Old Description",
            isPrivate: false,
            createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var request = new UpdateCollectionRequestDto
        {
            Name = "New Name",
            Description = "New Description",
            IsPrivate = true
        };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await GetCollectionFromDbAsync(collection.Guid);
        updated!.Name.Should().Be("New Name");
        updated.Description.Should().Be("New Description");
        updated.IsPrivate.Should().BeTrue();
        updated.UpdatedByUserGuid.Should().Be(user.Guid);
        updated.UpdatedDate.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Tests Update Collection – Just Name – Updates Name.
    /// </summary>
    [Fact]
    public async Task UpdateCollection_JustName_UpdatesName()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(
            name: "Old Name",
            isPrivate: false,
            createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var request = new UpdateCollectionRequestDto
        {
            Name = "New Name",
            Description = collection.Description,
            IsPrivate = collection.IsPrivate
        };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await GetCollectionFromDbAsync(collection.Guid);
        updated!.Name.Should().Be("New Name");
        updated.UpdatedByUserGuid.Should().Be(user.Guid);
    }

    /// <summary>
    /// Tests Update Collection – No Changes – Returns No Updated Data.
    /// </summary>
    [Fact]
    public async Task UpdateCollection_NoChanges_ReturnsNoUpdatedData()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(
            name: "Name",
            isPrivate: false,
            createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var request = new UpdateCollectionRequestDto
        {
            Name = collection.Name,
            Description = collection.Description,
            IsPrivate = collection.IsPrivate
        };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}", request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoUpdatedData);
    }

    /// <summary>
    /// Tests Update Collection – Collection Not Found – Returns Collection Not Found.
    /// </summary>
    [Fact]
    public async Task UpdateCollection_CollectionNotFound_ReturnsCollectionNotFound()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var request = new UpdateCollectionRequestDto
        {
            Name = "New Name",
            IsPrivate = false
        };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonCollectionRouting}/{Guid.CreateVersion7()}", request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.CollectionNotFound);
    }

    /// <summary>
    /// Tests Update Collection – As User On Other Users Collection – Returns Forbidden.
    /// </summary>
    [Fact]
    public async Task UpdateCollection_AsUserOnOtherUsersCollection_ReturnsForbidden()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var otherUser = await UserRepo.CreateUserAsync("other@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(
            name: "Owners Collection",
            isPrivate: false,
            createdByUserGuid: owner.Guid);

        AuthenticateAs(otherUser);

        var request = new UpdateCollectionRequestDto
        {
            Name = "Hijacked Name",
            IsPrivate = false
        };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}", request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    /// <summary>
    /// Tests Update Collection – As Admin – Can Update Any Collection.
    /// </summary>
    [Fact]
    public async Task UpdateCollection_AsAdmin_CanUpdateAnyCollection()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var collection = await CollectionRepo.CreateCollectionAsync(
            name: "Old Name",
            isPrivate: false,
            createdByUserGuid: owner.Guid);

        AuthenticateAs(admin);

        var request = new UpdateCollectionRequestDto
        {
            Name = "Updated By Admin",
            IsPrivate = true
        };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await GetCollectionFromDbAsync(collection.Guid);
        updated!.Name.Should().Be("Updated By Admin");
        updated.UpdatedByUserGuid.Should().Be(admin.Guid);
    }

    /// <summary>
    /// Tests Update Collection – Not Authenticated – Returns Unauthorized.
    /// </summary>
    [Fact]
    public async Task UpdateCollection_NotAuthenticated_ReturnsUnauthorized()
    {
        var request = new UpdateCollectionRequestDto { Name = "X", IsPrivate = false };
        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonCollectionRouting}/{Guid.CreateVersion7()}", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Tests Update Collection – Collection Is Not Active – Returns Object Is Not Active.
    /// </summary>
    [Fact]
    public async Task UpdateCollection_CollectionIsNotActive_ReturnsObjectIsNotActive()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(
            name: "Deleted Collection",
            isPrivate: false,
            createdByUserGuid: user.Guid,
            deletedByUserGuid: user.Guid);

        AuthenticateAs(user);

        var request = new UpdateCollectionRequestDto { Name = "New Name", IsPrivate = false };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}", request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.ObjectIsNotActive);
    }

    #endregion

    #region DeleteCollection

    /// <summary>
    /// Tests Delete Collection – As Owner – Soft Deletes Collection.
    /// </summary>
    [Fact]
    public async Task DeleteCollection_AsOwner_SoftDeletesCollection()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var response = await Client.DeleteAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleted = await GetCollectionFromDbAsync(collection.Guid);
        deleted.Should().NotBeNull();
        deleted!.DeletedByUserGuid.Should().Be(user.Guid);
        deleted.DeletionDate.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
        deleted.Status.Should().Be(CollectionStatus.Deleted);
    }

    /// <summary>
    /// Tests Delete Collection – As Admin – Can Soft Delete Any Collection.
    /// </summary>
    [Fact]
    public async Task DeleteCollection_AsAdmin_CanSoftDeleteAnyCollection()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var collection = await CollectionRepo.CreateCollectionAsync(createdByUserGuid: owner.Guid);

        AuthenticateAs(admin);

        var response = await Client.DeleteAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleted = await GetCollectionFromDbAsync(collection.Guid);
        deleted.Should().NotBeNull();
        deleted!.DeletedByUserGuid.Should().Be(admin.Guid);
        deleted.DeletionDate.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
        deleted.Status.Should().Be(CollectionStatus.Deleted);
    }

    /// <summary>
    /// Tests Delete Collection – As User On Other Users Collection – Returns Forbidden.
    /// </summary>
    [Fact]
    public async Task DeleteCollection_AsUserOnOtherUsersCollection_ReturnsForbidden()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var otherUser = await UserRepo.CreateUserAsync("other@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(createdByUserGuid: owner.Guid);

        AuthenticateAs(otherUser);

        var response = await Client.DeleteAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    /// <summary>
    /// Tests Delete Collection – Collection Not Found – Returns Collection Not Found.
    /// </summary>
    [Fact]
    public async Task DeleteCollection_CollectionNotFound_ReturnsCollectionNotFound()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var response = await Client.DeleteAsync(
            $"{Routings.CommonCollectionRouting}/{Guid.CreateVersion7()}");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.CollectionNotFound);
    }

    /// <summary>
    /// Tests Delete Collection – Not Authenticated – Returns Unauthorized.
    /// </summary>
    [Fact]
    public async Task DeleteCollection_NotAuthenticated_ReturnsUnauthorized()
    {
        var response = await Client.DeleteAsync(
            $"{Routings.CommonCollectionRouting}/{Guid.CreateVersion7()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region ActivateCollection

    /// <summary>
    /// Tests Activate Collection – As Owner – Activates Collection.
    /// </summary>
    [Fact]
    public async Task ActivateCollection_AsOwner_ActivatesCollection()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(
            createdByUserGuid: user.Guid,
            deletedByUserGuid: user.Guid);

        AuthenticateAs(user);

        var response = await Client.PostAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var activated = await GetCollectionFromDbAsync(collection.Guid);
        activated!.DeletionDate.Should().BeNull();
        activated.DeletedByUserGuid.Should().BeNull();
        activated.UpdatedByUserGuid.Should().Be(user.Guid);
        activated.Status.Should().Be(CollectionStatus.Active);
    }

    /// <summary>
    /// Tests Activate Collection – As Admin – Can Activate Any Collection.
    /// </summary>
    [Fact]
    public async Task ActivateCollection_AsAdmin_CanActivateAnyCollection()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var collection = await CollectionRepo.CreateCollectionAsync(
            createdByUserGuid: owner.Guid,
            deletedByUserGuid: owner.Guid);

        AuthenticateAs(admin);

        var response = await Client.PostAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var activated = await GetCollectionFromDbAsync(collection.Guid);
        activated!.DeletionDate.Should().BeNull();
        activated.DeletedByUserGuid.Should().BeNull();
        activated.UpdatedByUserGuid.Should().Be(admin.Guid);
        activated.Status.Should().Be(CollectionStatus.Active);
    }

    /// <summary>
    /// Tests Activate Collection – As User On Other Users Collection – Returns Forbidden.
    /// </summary>
    [Fact]
    public async Task ActivateCollection_AsUserOnOtherUsersCollection_ReturnsForbidden()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var otherUser = await UserRepo.CreateUserAsync("other@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(
            createdByUserGuid: owner.Guid,
            deletedByUserGuid: owner.Guid);

        AuthenticateAs(otherUser);

        var response = await Client.PostAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}/activate", null);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    /// <summary>
    /// Tests Activate Collection – Collection Not Found – Returns Collection Not Found.
    /// </summary>
    [Fact]
    public async Task ActivateCollection_CollectionNotFound_ReturnsCollectionNotFound()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var response = await Client.PostAsync(
            $"{Routings.CommonCollectionRouting}/{Guid.CreateVersion7()}/activate", null);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.CollectionNotFound);
    }

    /// <summary>
    /// Tests Activate Collection – Not Authenticated – Returns Unauthorized.
    /// </summary>
    [Fact]
    public async Task ActivateCollection_NotAuthenticated_ReturnsUnauthorized()
    {
        var response = await Client.PostAsync(
            $"{Routings.CommonCollectionRouting}/{Guid.CreateVersion7()}/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region AddSpecimenToCollection

    /// <summary>
    /// Tests Add Specimen To Collection – Valid Request – Specimen Linked To Collection.
    /// </summary>
    [Fact]
    public async Task AddSpecimenToCollection_ValidRequest_SpecimenLinkedToCollection()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid);
        var collection = await CollectionRepo.CreateCollectionAsync(createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var response = await Client.PostAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}/specimen/{specimen.Guid}", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var link = await GetSpecimenCollectionFromDbAsync(collection.Guid, specimen.Guid);
        link.Should().NotBeNull();
    }

    /// <summary>
    /// Tests Add Specimen To Collection – Collection Not Found – Returns Collection Not Found.
    /// </summary>
    [Fact]
    public async Task AddSpecimenToCollection_CollectionNotFound_ReturnsCollectionNotFound()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid);

        AuthenticateAs(user);

        var response = await Client.PostAsync(
            $"{Routings.CommonCollectionRouting}/{Guid.CreateVersion7()}/specimen/{specimen.Guid}", null);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.CollectionNotFound);
    }

    /// <summary>
    /// Tests Add Specimen To Collection – Specimen Not Found – Returns No Specimen Found.
    /// </summary>
    [Fact]
    public async Task AddSpecimenToCollection_SpecimenNotFound_ReturnsNoSpecimenFound()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var response = await Client.PostAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}/specimen/{Guid.CreateVersion7()}", null);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoSpecimenFound);
    }

    /// <summary>
    /// Tests Add Specimen To Collection – Specimen Already In Collection – Returns Error.
    /// </summary>
    [Fact]
    public async Task AddSpecimenToCollection_SpecimenAlreadyInCollection_ReturnsError()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid);
        var collection = await CollectionRepo.CreateCollectionAsync(createdByUserGuid: user.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, specimen.Guid);

        AuthenticateAs(user);

        var response = await Client.PostAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}/specimen/{specimen.Guid}", null);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.SpecimenAlreadyInCollection);
    }

    /// <summary>
    /// Tests Add Specimen To Collection – Collection Is Not Active – Returns Object Is Not Active.
    /// </summary>
    [Fact]
    public async Task AddSpecimenToCollection_CollectionIsNotActive_ReturnsObjectIsNotActive()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid);
        var collection = await CollectionRepo.CreateCollectionAsync(
            createdByUserGuid: user.Guid,
            deletedByUserGuid: user.Guid);

        AuthenticateAs(user);

        var response = await Client.PostAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}/specimen/{specimen.Guid}", null);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.ObjectIsNotActive);
    }

    /// <summary>
    /// Tests Add Specimen To Collection – As User On Other Users Collection – Returns Forbidden.
    /// </summary>
    [Fact]
    public async Task AddSpecimenToCollection_AsUserOnOtherUsersCollection_ReturnsForbidden()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var otherUser = await UserRepo.CreateUserAsync("other@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid);
        var collection = await CollectionRepo.CreateCollectionAsync(createdByUserGuid: owner.Guid);

        AuthenticateAs(otherUser);

        var response = await Client.PostAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}/specimen/{specimen.Guid}", null);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    /// <summary>
    /// Tests Add Specimen To Collection – Not Authenticated – Returns Unauthorized.
    /// </summary>
    [Fact]
    public async Task AddSpecimenToCollection_NotAuthenticated_ReturnsUnauthorized()
    {
        var response = await Client.PostAsync(
            $"{Routings.CommonCollectionRouting}/{Guid.CreateVersion7()}/specimen/{Guid.CreateVersion7()}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region RemoveSpecimenFromCollection

    /// <summary>
    /// Tests Remove Specimen From Collection – Valid Request – Specimen Removed From Collection.
    /// </summary>
    [Fact]
    public async Task RemoveSpecimenFromCollection_ValidRequest_SpecimenRemovedFromCollection()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid);
        var collection = await CollectionRepo.CreateCollectionAsync(createdByUserGuid: user.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, specimen.Guid);

        AuthenticateAs(user);

        var response = await Client.DeleteAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}/specimen/{specimen.Guid}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var link = await GetSpecimenCollectionFromDbAsync(collection.Guid, specimen.Guid);
        link.Should().BeNull();
    }

    /// <summary>
    /// Tests Remove Specimen From Collection – Collection Not Found – Returns Collection Not Found.
    /// </summary>
    [Fact]
    public async Task RemoveSpecimenFromCollection_CollectionNotFound_ReturnsCollectionNotFound()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid);

        AuthenticateAs(user);

        var response = await Client.DeleteAsync(
            $"{Routings.CommonCollectionRouting}/{Guid.CreateVersion7()}/specimen/{specimen.Guid}");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.CollectionNotFound);
    }

    /// <summary>
    /// Tests Remove Specimen From Collection – As User On Other Users Collection – Returns Forbidden.
    /// </summary>
    [Fact]
    public async Task RemoveSpecimenFromCollection_AsUserOnOtherUsersCollection_ReturnsForbidden()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var otherUser = await UserRepo.CreateUserAsync("other@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid);
        var collection = await CollectionRepo.CreateCollectionAsync(createdByUserGuid: owner.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, specimen.Guid);

        AuthenticateAs(otherUser);

        var response = await Client.DeleteAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}/specimen/{specimen.Guid}");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    /// <summary>
    /// Tests Remove Specimen From Collection – Collection Is Not Active – Returns Object Is Not Active.
    /// </summary>
    [Fact]
    public async Task RemoveSpecimenFromCollection_CollectionIsNotActive_ReturnsObjectIsNotActive()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid);
        var collection = await CollectionRepo.CreateCollectionAsync(
            createdByUserGuid: user.Guid,
            deletedByUserGuid: user.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, specimen.Guid);

        AuthenticateAs(user);

        var response = await Client.DeleteAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}/specimen/{specimen.Guid}");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.ObjectIsNotActive);
    }

    /// <summary>
    /// Tests Remove Specimen From Collection – Specimen Not In Collection – Returns Error.
    /// </summary>
    [Fact]
    public async Task RemoveSpecimenFromCollection_SpecimenNotInCollection_ReturnsError()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid);
        var collection = await CollectionRepo.CreateCollectionAsync(createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var response = await Client.DeleteAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}/specimen/{specimen.Guid}");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.SpecimenNotInCollection);
    }

    /// <summary>
    /// Tests Remove Specimen From Collection – Not Authenticated – Returns Unauthorized.
    /// </summary>
    [Fact]
    public async Task RemoveSpecimenFromCollection_NotAuthenticated_ReturnsUnauthorized()
    {
        var response = await Client.DeleteAsync(
            $"{Routings.CommonCollectionRouting}/{Guid.CreateVersion7()}/specimen/{Guid.CreateVersion7()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GetAllSpecimensInCollection

    /// <summary>
    /// Tests Get All Specimens In Collection – Filter By Name – Returns Only Matching Specimens.
    /// </summary>
    [Fact]
    public async Task GetAllSpecimensInCollection_FilterByName_ReturnsOnlyMatchingSpecimens()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var collection = await CollectionRepo.CreateCollectionAsync(isPrivate: false);

        var lion = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, name: "Lion");
        var tiger = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, name: "Tiger");
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, lion.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, tiger.Guid);

        AuthenticateAs(user);

        var response = await Client.GetAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}/specimens?filterByName=Lion");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetAllSpecimenInCollectionResponseDto>();
        result!.TotalCount.Should().Be(1);
        result.Specimens.Should().ContainSingle(s => s.SpecimenGuid == lion.Guid);
    }

    /// <summary>
    /// Tests Get All Specimens In Collection – Inactive Specimens – Are Excluded For The Owner.
    /// </summary>
    [Fact]
    public async Task GetAllSpecimensInCollection_InactiveSpecimens_AreExcludedForOwner()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var collection = await CollectionRepo.CreateCollectionAsync(isPrivate: false, createdByUserGuid: user.Guid);

        var active = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid,
            SpecimenStatus.Active, createdByUserGuid: user.Guid);
        var pending = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid,
            SpecimenStatus.Pending, createdByUserGuid: user.Guid);
        var deleted = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid,
            SpecimenStatus.Deleted, createdByUserGuid: user.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, active.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, pending.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, deleted.Guid);

        AuthenticateAs(user);

        var response = await Client.GetAsync($"{Routings.CommonCollectionRouting}/{collection.Guid}/specimens");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetAllSpecimenInCollectionResponseDto>();
        // Only the active specimen is returned; pending and deleted ones are excluded even for the owner.
        result!.TotalCount.Should().Be(1);
        result.Specimens.Single().SpecimenGuid.Should().Be(active.Guid);
    }

    /// <summary>
    /// Tests Get All Specimens In Collection – Inactive Specimens – Are Excluded For Admins.
    /// </summary>
    [Fact]
    public async Task GetAllSpecimensInCollection_InactiveSpecimens_AreExcludedForAdmin()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var collection = await CollectionRepo.CreateCollectionAsync(isPrivate: false, createdByUserGuid: owner.Guid);

        var active = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid,
            SpecimenStatus.Active, createdByUserGuid: owner.Guid);
        var deleted = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid,
            SpecimenStatus.Deleted, createdByUserGuid: owner.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, active.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, deleted.Guid);

        AuthenticateAs(admin);

        var response = await Client.GetAsync($"{Routings.CommonCollectionRouting}/{collection.Guid}/specimens");

        var result = await response.Content.ReadFromJsonAsync<GetAllSpecimenInCollectionResponseDto>();
        // Admins see all visibilities but still never inactive specimens.
        result!.TotalCount.Should().Be(1);
        result.Specimens.Single().SpecimenGuid.Should().Be(active.Guid);
    }

    /// <summary>
    /// Tests Get All Specimens In Collection – Anonymous – Returns Only Active Public Specimens.
    /// </summary>
    [Fact]
    public async Task GetAllSpecimensInCollection_Anonymous_ReturnsOnlyActivePublicSpecimens()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var collection = await CollectionRepo.CreateCollectionAsync(isPrivate: false, createdByUserGuid: owner.Guid);

        var publicActive = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid,
            isPublic: true, createdByUserGuid: owner.Guid);
        var privateActive = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid,
            isPublic: false, createdByUserGuid: owner.Guid);
        var publicInactive = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid,
            SpecimenStatus.Pending, isPublic: true, createdByUserGuid: owner.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, publicActive.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, privateActive.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, publicInactive.Guid);

        var response = await Client.GetAsync($"{Routings.CommonCollectionRouting}/{collection.Guid}/specimens");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetAllSpecimenInCollectionResponseDto>();
        // Anonymous callers only see the active public specimen.
        result!.TotalCount.Should().Be(1);
        result.Specimens.Single().SpecimenGuid.Should().Be(publicActive.Guid);
    }

    /// <summary>
    /// Tests Get All Specimens In Collection – Public Collection – Returns Public Specimens.
    /// </summary>
    [Fact]
    public async Task GetAllSpecimensInCollection_PublicCollection_ReturnsPublicSpecimens()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var collection = await CollectionRepo.CreateCollectionAsync(isPrivate: false);
        var sp = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, sp.Guid);

        AuthenticateAs(user);

        var response = await Client.GetAsync($"{Routings.CommonCollectionRouting}/{collection.Guid}/specimens");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetAllSpecimenInCollectionResponseDto>();
        result!.TotalCount.Should().Be(1);
        result.Specimens.Should().HaveCount(1);
    }

    /// <summary>
    /// Tests Get All Specimens In Collection – Private Specimens Of Other User – Are Hidden.
    /// </summary>
    [Fact]
    public async Task GetAllSpecimensInCollection_PrivateSpecimensOfOtherUser_AreHidden()
    {
        var viewer = await UserRepo.CreateUserAsync("viewer@test.de");
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var collection = await CollectionRepo.CreateCollectionAsync(isPrivate: false);
        var publicSp = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true, createdByUserGuid: owner.Guid);
        var privateSp = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: false, createdByUserGuid: owner.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, publicSp.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, privateSp.Guid);

        AuthenticateAs(viewer);

        var response = await Client.GetAsync($"{Routings.CommonCollectionRouting}/{collection.Guid}/specimens");

        var result = await response.Content.ReadFromJsonAsync<GetAllSpecimenInCollectionResponseDto>();
        result!.TotalCount.Should().Be(1);
        result.Specimens.Should().HaveCount(1);
        result.Specimens.Single().SpecimenGuid.Should().Be(publicSp.Guid);
    }

    /// <summary>
    /// Tests Get All Specimens In Collection – Own Private Specimens – Are Visible.
    /// </summary>
    [Fact]
    public async Task GetAllSpecimensInCollection_OwnPrivateSpecimens_AreVisible()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var collection = await CollectionRepo.CreateCollectionAsync(isPrivate: false);
        var privateSp = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: false, createdByUserGuid: user.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, privateSp.Guid);

        AuthenticateAs(user);

        var response = await Client.GetAsync($"{Routings.CommonCollectionRouting}/{collection.Guid}/specimens");

        var result = await response.Content.ReadFromJsonAsync<GetAllSpecimenInCollectionResponseDto>();
        result!.TotalCount.Should().Be(1);
        result.Specimens.Single().SpecimenGuid.Should().Be(privateSp.Guid);
    }

    /// <summary>
    /// Tests Get All Specimens In Collection – Private Collection – Owner – Can Access.
    /// </summary>
    [Fact]
    public async Task GetAllSpecimensInCollection_PrivateCollection_Owner_CanAccess()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var collection = await CollectionRepo.CreateCollectionAsync(isPrivate: true, createdByUserGuid: owner.Guid);
        var sp = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, sp.Guid);

        AuthenticateAs(owner);

        var response = await Client.GetAsync($"{Routings.CommonCollectionRouting}/{collection.Guid}/specimens");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetAllSpecimenInCollectionResponseDto>();
        result!.Specimens.Should().HaveCount(1);
    }

    /// <summary>
    /// Tests Get All Specimens In Collection – Private Collection – Other User – Returns Forbidden.
    /// </summary>
    [Fact]
    public async Task GetAllSpecimensInCollection_PrivateCollection_OtherUser_ReturnsForbidden()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var other = await UserRepo.CreateUserAsync("other@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(isPrivate: true, createdByUserGuid: owner.Guid);

        AuthenticateAs(other);

        var response = await Client.GetAsync($"{Routings.CommonCollectionRouting}/{collection.Guid}/specimens");
        
        var error = await ReadErrorAsync(response);
        
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    /// <summary>
    /// Tests Get All Specimens In Collection – Admin – Sees All Specimens.
    /// </summary>
    [Fact]
    public async Task GetAllSpecimensInCollection_Admin_SeesAllSpecimens()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var collection = await CollectionRepo.CreateCollectionAsync(isPrivate: false, createdByUserGuid: owner.Guid);
        var publicSp = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true, createdByUserGuid: owner.Guid);
        var privateSp = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: false, createdByUserGuid: owner.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, publicSp.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, privateSp.Guid);

        AuthenticateAs(admin);

        var response = await Client.GetAsync($"{Routings.CommonCollectionRouting}/{collection.Guid}/specimens");

        var result = await response.Content.ReadFromJsonAsync<GetAllSpecimenInCollectionResponseDto>();
        result!.TotalCount.Should().Be(2);
        result.Specimens.Should().HaveCount(2);
    }

    /// <summary>
    /// Tests Get All Specimens In Collection – Deleted Collection – Non Owner – Returns Forbidden.
    /// </summary>
    [Fact]
    public async Task GetAllSpecimensInCollection_DeletedCollection_NonOwner_ReturnsForbidden()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var other = await UserRepo.CreateUserAsync("other@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(
            isPrivate: false,
            createdByUserGuid: owner.Guid,
            deletedByUserGuid: owner.Guid);

        AuthenticateAs(other);

        var response = await Client.GetAsync($"{Routings.CommonCollectionRouting}/{collection.Guid}/specimens");

        var error = await ReadErrorAsync(response);
        
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    /// <summary>
    /// Tests Get All Specimens In Collection – Unknown Guid – Returns Not Found.
    /// </summary>
    [Fact]
    public async Task GetAllSpecimensInCollection_UnknownGuid_ReturnsNotFound()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var response = await Client.GetAsync($"{Routings.CommonCollectionRouting}/{Guid.NewGuid()}/specimens");

        var error = await ReadErrorAsync(response);
        
        error.ErrorCode.Should().Be((int)ErrorCode.CollectionNotFound);
    }

    /// <summary>
    /// Tests Get All Specimens In Collection – Paging – Returns Correct Page.
    /// </summary>
    [Fact]
    public async Task GetAllSpecimensInCollection_Paging_ReturnsCorrectPage()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var collection = await CollectionRepo.CreateCollectionAsync(isPrivate: false);

        for (var i = 0; i < 5; i++)
        {
            var sp = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true);
            await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, sp.Guid);
        }

        AuthenticateAs(user);

        var response = await Client.GetAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}/specimens?page=2&pageSize=2");

        var result = await response.Content.ReadFromJsonAsync<GetAllSpecimenInCollectionResponseDto>();
        result!.TotalCount.Should().Be(5);
        result.Specimens.Should().HaveCount(2);
    }

    /// <summary>
    /// Tests Get All Specimens In Collection – Sort By Name – Desc – Returns Sorted List.
    /// </summary>
    [Fact]
    public async Task GetAllSpecimensInCollection_SortByName_Desc_ReturnsSortedList()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var collection = await CollectionRepo.CreateCollectionAsync(isPrivate: false);

        var sp1 = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true);
        var sp2 = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true);
        var sp3 = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, sp1.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, sp2.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, sp3.Guid);

        AuthenticateAs(user);

        var response = await Client.GetAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}/specimens?sortBy=name&sortDirection=DESC");

        var result = await response.Content.ReadFromJsonAsync<GetAllSpecimenInCollectionResponseDto>();
        var names = result!.Specimens.Select(s => s.Name).ToList();
        names.Should().BeInDescendingOrder();
    }

    /// <summary>
    /// Tests Get All Specimens In Collection – Contains Collection Metadata.
    /// </summary>
    [Fact]
    public async Task GetAllSpecimensInCollection_ContainsCollectionMetadata()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(
            name: "My Birds",
            isPrivate: false,
            createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var response = await Client.GetAsync($"{Routings.CommonCollectionRouting}/{collection.Guid}/specimens");

        var result = await response.Content.ReadFromJsonAsync<GetAllSpecimenInCollectionResponseDto>();
        result!.CollectionGuid.Should().Be(collection.Guid);
        result.CollectionName.Should().Be("My Birds");
        result.CreatedUserGuid.Should().Be(user.Guid);
        result.Status.Should().Be(CollectionStatus.Active);
        result.CreatedAt.Should().NotBeNull();
    }

    #endregion

    #region GetAllSpecimensInCollection – Static Collections

    /// <summary>
    /// Tests Get All Specimens In Collection – Static All Specimens – Returns Public Specimens.
    /// </summary>
    [Fact]
    public async Task GetAllSpecimensInCollection_StaticAllSpecimens_ReturnsPublicSpecimens()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        var other = await UserRepo.CreateUserAsync("other@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();

        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true, createdByUserGuid: user.Guid);
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true, createdByUserGuid: other.Guid);
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: false, createdByUserGuid: other.Guid);

        AuthenticateAs(user);

        var response = await Client.GetAsync(
            $"{Routings.CommonCollectionRouting}/00000000-0000-0000-0000-000000000001/specimens");

        var result = await response.Content.ReadFromJsonAsync<GetAllSpecimenInCollectionResponseDto>();
        result!.TotalCount.Should().Be(2);
        result.CollectionName.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Tests Get All Specimens In Collection – Static My Active – Returns Own Active Specimens Only.
    /// </summary>
    [Fact]
    public async Task GetAllSpecimensInCollection_StaticMyActive_ReturnsOwnActiveSpecimensOnly()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        var other = await UserRepo.CreateUserAsync("other@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();

        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, SpecimenStatus.Active, createdByUserGuid: user.Guid);
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, SpecimenStatus.Pending, createdByUserGuid: user.Guid);
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, SpecimenStatus.Active, createdByUserGuid: other.Guid);

        AuthenticateAs(user);

        var response = await Client.GetAsync(
            $"{Routings.CommonCollectionRouting}/00000000-0000-0000-0000-000000000003/specimens");

        var result = await response.Content.ReadFromJsonAsync<GetAllSpecimenInCollectionResponseDto>();
        result!.TotalCount.Should().Be(1);
    }

    /// <summary>
    /// Tests Get All Specimens In Collection – Static My Deleted – Returns Own Deleted Specimens.
    /// </summary>
    [Fact]
    public async Task GetAllSpecimensInCollection_StaticMyDeleted_ReturnsOwnDeletedSpecimens()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();

        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, SpecimenStatus.Active, createdByUserGuid: user.Guid);
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, SpecimenStatus.Deleted, createdByUserGuid: user.Guid);
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, SpecimenStatus.Deleted, createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var response = await Client.GetAsync(
            $"{Routings.CommonCollectionRouting}/00000000-0000-0000-0000-000000000005/specimens");

        var result = await response.Content.ReadFromJsonAsync<GetAllSpecimenInCollectionResponseDto>();
        result!.TotalCount.Should().Be(2);
    }

    #endregion

    #region GetAllCollections – Anonymous

    /// <summary>
    /// Tests Get All Collections – Anonymous – Allows Unauthenticated Access.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_Anonymous_AllowsUnauthenticatedAccess()
    {
        await CollectionRepo.CreateCollectionAsync(name: "Public", isPrivate: false);

        var response = await Client.GetAsync(Routings.CommonCollectionRouting);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        // The public dynamic collection plus the "All Specimens" static collection (visible to everyone).
        result!.TotalCount.Should().Be(2);
        result.Collections.Should().HaveCount(2);
        result.Collections.Should().Contain(c => c.Guid == new Guid("00000000-0000-0000-0000-000000000001"));
    }

    /// <summary>
    /// Tests Get All Collections – Anonymous – Contains Only The All-Specimens Static Collection.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_Anonymous_ContainsOnlyAllSpecimensStaticCollection()
    {
        var response = await Client.GetAsync(Routings.CommonCollectionRouting);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        // Anonymous callers see the public "All Specimens" static collection but none of the user-specific ones.
        result!.TotalCount.Should().Be(1);
        result.Collections.Should().ContainSingle()
            .Which.Guid.Should().Be(new Guid("00000000-0000-0000-0000-000000000001"));
        result.Collections.Should().NotContain(c => c.Guid == new Guid("00000000-0000-0000-0000-000000000002"));
    }

    /// <summary>
    /// Tests Get All Collections – Anonymous – Dynamic Collection Counts Only Active Public Specimens.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_Anonymous_DynamicCollection_OnlyCountsActivePublicSpecimens()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("Felidae", TaxonomyRank.Family);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var collection = await CollectionRepo.CreateCollectionAsync(
            name: "Mixed", isPrivate: false, createdByUserGuid: owner.Guid);

        var publicActive = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid,
            isPublic: true, createdByUserGuid: owner.Guid);
        var privateActive = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid,
            isPublic: false, createdByUserGuid: owner.Guid);
        var publicInactive = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid,
            SpecimenStatus.Pending, isPublic: true, createdByUserGuid: owner.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, publicActive.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, privateActive.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, publicInactive.Guid);

        var response = await Client.GetAsync(Routings.CommonCollectionRouting);

        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        // Only the active public specimen counts; the private and the inactive one are excluded.
        result!.Collections.Single(c => c.Name == "Mixed").SpecimenCount.Should().Be(1);
    }

    /// <summary>
    /// Tests Get All Collections – Anonymous – Private Collection Is Not Returned.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_Anonymous_PrivateCollectionIsNotReturned()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        await CollectionRepo.CreateCollectionAsync(name: "Secret", isPrivate: true, createdByUserGuid: owner.Guid);
        await CollectionRepo.CreateCollectionAsync(name: "Open", isPrivate: false, createdByUserGuid: owner.Guid);

        var response = await Client.GetAsync(Routings.CommonCollectionRouting);

        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        // The public collection plus the "All Specimens" static collection; the private one stays hidden.
        result!.TotalCount.Should().Be(2);
        result.Collections.Should().Contain(c => c.Name == "Open");
        result.Collections.Should().NotContain(c => c.Name == "Secret");
    }

    /// <summary>
    /// Tests Get All Collections – Anonymous – Paging – Returns Total Count And Correct Page.
    /// </summary>
    [Fact]
    public async Task GetAllCollections_Anonymous_Paging_ReturnsTotalCountAndCorrectPage()
    {
        for (var i = 0; i < 5; i++)
        {
            await CollectionRepo.CreateCollectionAsync(name: $"Col {i}", isPrivate: false);
        }

        var response = await Client.GetAsync(
            $"{Routings.CommonCollectionRouting}?page=1&pageSize=3");

        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        // 5 dynamic collections plus the "All Specimens" static collection.
        result!.TotalCount.Should().Be(6);
        result.Collections.Should().HaveCount(3);
    }

    #endregion

    #region GetAllSpecimensInCollection – Anonymous

    /// <summary>
    /// Tests Get All Specimens In Collection – Anonymous – Public Collection – Only Public Specimens Returned.
    /// </summary>
    [Fact]
    public async Task GetAllSpecimensInCollection_Anonymous_PublicCollection_OnlyPublicSpecimensReturned()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var collection = await CollectionRepo.CreateCollectionAsync(isPrivate: false, createdByUserGuid: owner.Guid);
        var publicSp = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true, createdByUserGuid: owner.Guid);
        var privateSp = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: false, createdByUserGuid: owner.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, publicSp.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, privateSp.Guid);

        var response = await Client.GetAsync($"{Routings.CommonCollectionRouting}/{collection.Guid}/specimens");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetAllSpecimenInCollectionResponseDto>();
        result!.TotalCount.Should().Be(1);
        result.Specimens.Single().SpecimenGuid.Should().Be(publicSp.Guid);
    }

    /// <summary>
    /// Tests Get All Specimens In Collection – Anonymous – Private Collection – Returns Forbidden.
    /// </summary>
    [Fact]
    public async Task GetAllSpecimensInCollection_Anonymous_PrivateCollection_ReturnsForbidden()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(isPrivate: true, createdByUserGuid: owner.Guid);

        var response = await Client.GetAsync($"{Routings.CommonCollectionRouting}/{collection.Guid}/specimens");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    /// <summary>
    /// Tests Get All Specimens In Collection – Anonymous – Static All Specimens – Returns Public Specimens.
    /// </summary>
    [Fact]
    public async Task GetAllSpecimensInCollection_Anonymous_StaticAllSpecimens_ReturnsPublicSpecimens()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var publicSp = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true, createdByUserGuid: owner.Guid);
        await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: false, createdByUserGuid: owner.Guid);

        var response = await Client.GetAsync(
            $"{Routings.CommonCollectionRouting}/00000000-0000-0000-0000-000000000001/specimens");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetAllSpecimenInCollectionResponseDto>();
        result!.TotalCount.Should().Be(1);
        result.Specimens.Single().SpecimenGuid.Should().Be(publicSp.Guid);
    }

    /// <summary>
    /// Tests Get All Specimens In Collection – Anonymous – User-Specific Static Collection – Returns Forbidden.
    /// </summary>
    [Fact]
    public async Task GetAllSpecimensInCollection_Anonymous_UserSpecificStaticCollection_ReturnsForbidden()
    {
        // "My Active" (00...003) is tied to a user and still requires authentication.
        var response = await Client.GetAsync(
            $"{Routings.CommonCollectionRouting}/00000000-0000-0000-0000-000000000003/specimens");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    /// <summary>
    /// Tests Get All Specimens In Collection – Anonymous – Deleted Public Collection – Returns Forbidden.
    /// </summary>
    [Fact]
    public async Task GetAllSpecimensInCollection_Anonymous_DeletedPublicCollection_ReturnsForbidden()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(
            isPrivate: false,
            createdByUserGuid: owner.Guid,
            deletedByUserGuid: owner.Guid);

        var response = await Client.GetAsync($"{Routings.CommonCollectionRouting}/{collection.Guid}/specimens");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    #endregion
}
