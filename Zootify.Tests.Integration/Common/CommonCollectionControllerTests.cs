using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Common;

public class CommonCollectionControllerTests : IntegrationTestBase
{
    public CommonCollectionControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    #region CreateCollection

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

    [Fact]
    public async Task CreateCollection_NotAuthenticated_ReturnsUnauthorized()
    {
        var request = new CreateCollectionRequestDto { Name = "X", IsPrivate = false };
        var response = await Client.PostAsJsonAsync(Routings.CommonCollectionRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GetAllCollections

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

    [Fact]
    public async Task GetAllCollections_FilterByName_CaseInsensitiveOnStaticCollections()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var response = await Client.GetAsync($"{Routings.CommonCollectionRouting}?filterByName=alle");

        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        result!.Collections.Should().Contain(c => c.Name == "Alle Exemplare");
    }

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

    #endregion

    #region UpdateCollection

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

    [Fact]
    public async Task UpdateCollection_NotAuthenticated_ReturnsUnauthorized()
    {
        var request = new UpdateCollectionRequestDto { Name = "X", IsPrivate = false };
        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonCollectionRouting}/{Guid.CreateVersion7()}", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region DeleteCollection

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

    [Fact]
    public async Task DeleteCollection_NotAuthenticated_ReturnsUnauthorized()
    {
        var response = await Client.DeleteAsync(
            $"{Routings.CommonCollectionRouting}/{Guid.CreateVersion7()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region ActivateCollection

    [Fact]
    public async Task ActivateCollection_AsOwner_ActivatesCollection()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(
            createdByUserGuid: user.Guid,
            deletedByUserGuid: user.Guid);

        AuthenticateAs(user);

        var response = await Client.PostAsync(
            $"{Routings.CommonCollectionRouting}/activate/{collection.Guid}", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var activated = await GetCollectionFromDbAsync(collection.Guid);
        activated!.DeletionDate.Should().BeNull();
        activated.DeletedByUserGuid.Should().BeNull();
        activated.UpdatedByUserGuid.Should().Be(user.Guid);
        activated.Status.Should().Be(CollectionStatus.Active);
    }

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
            $"{Routings.CommonCollectionRouting}/activate/{collection.Guid}", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var activated = await GetCollectionFromDbAsync(collection.Guid);
        activated!.DeletionDate.Should().BeNull();
        activated.DeletedByUserGuid.Should().BeNull();
        activated.UpdatedByUserGuid.Should().Be(admin.Guid);
        activated.Status.Should().Be(CollectionStatus.Active);
    }

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
            $"{Routings.CommonCollectionRouting}/activate/{collection.Guid}", null);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    [Fact]
    public async Task ActivateCollection_CollectionNotFound_ReturnsCollectionNotFound()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var response = await Client.PostAsync(
            $"{Routings.CommonCollectionRouting}/activate/{Guid.CreateVersion7()}", null);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.CollectionNotFound);
    }

    [Fact]
    public async Task ActivateCollection_NotAuthenticated_ReturnsUnauthorized()
    {
        var response = await Client.PostAsync(
            $"{Routings.CommonCollectionRouting}/activate/{Guid.CreateVersion7()}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region AddSpecimenToCollection

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

    [Fact]
    public async Task AddSpecimenToCollection_SpecimenNotFound_ReturnsNoSpecimenWithThisGuid()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(createdByUserGuid: user.Guid);

        AuthenticateAs(user);

        var response = await Client.PostAsync(
            $"{Routings.CommonCollectionRouting}/{collection.Guid}/specimen/{Guid.CreateVersion7()}", null);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoSpecimenWithThisGuid);
    }

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

    [Fact]
    public async Task AddSpecimenToCollection_NotAuthenticated_ReturnsUnauthorized()
    {
        var response = await Client.PostAsync(
            $"{Routings.CommonCollectionRouting}/{Guid.CreateVersion7()}/specimen/{Guid.CreateVersion7()}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region RemoveSpecimenFromCollection

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

    [Fact]
    public async Task RemoveSpecimenFromCollection_NotAuthenticated_ReturnsUnauthorized()
    {
        var response = await Client.DeleteAsync(
            $"{Routings.CommonCollectionRouting}/{Guid.CreateVersion7()}/specimen/{Guid.CreateVersion7()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GetAllSpecimensInCollection

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

    [Fact]
    public async Task GetAllSpecimensInCollection_UnknownGuid_ReturnsNotFound()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var response = await Client.GetAsync($"{Routings.CommonCollectionRouting}/{Guid.NewGuid()}/specimens");

        var error = await ReadErrorAsync(response);
        
        error.ErrorCode.Should().Be((int)ErrorCode.CollectionNotFound);
    }

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
        result.Status.Should().Be(CollectionStatus.Active);
        result.CreatedAt.Should().NotBeNull();
    }

    #endregion

    #region GetAllSpecimensInCollection – Static Collections

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

    [Fact]
    public async Task GetAllCollections_Anonymous_AllowsUnauthenticatedAccess()
    {
        await CollectionRepo.CreateCollectionAsync(name: "Public", isPrivate: false);

        var response = await Client.GetAsync(Routings.CommonCollectionRouting);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        result!.TotalCount.Should().Be(1);
        result.Collections.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAllCollections_Anonymous_ContainsNoStaticCollections()
    {
        var response = await Client.GetAsync(Routings.CommonCollectionRouting);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        result!.TotalCount.Should().Be(0);
        result.Collections.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllCollections_Anonymous_PrivateCollectionIsNotReturned()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        await CollectionRepo.CreateCollectionAsync(name: "Secret", isPrivate: true, createdByUserGuid: owner.Guid);
        await CollectionRepo.CreateCollectionAsync(name: "Open", isPrivate: false, createdByUserGuid: owner.Guid);

        var response = await Client.GetAsync(Routings.CommonCollectionRouting);

        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        result!.TotalCount.Should().Be(1);
        result.Collections.Should().Contain(c => c.Name == "Open");
        result.Collections.Should().NotContain(c => c.Name == "Secret");
    }

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
        result!.TotalCount.Should().Be(5);
        result.Collections.Should().HaveCount(3);
    }

    #endregion

    #region GetAllSpecimensInCollection – Anonymous

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

    [Fact]
    public async Task GetAllSpecimensInCollection_Anonymous_PrivateCollection_ReturnsForbidden()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(isPrivate: true, createdByUserGuid: owner.Guid);

        var response = await Client.GetAsync($"{Routings.CommonCollectionRouting}/{collection.Guid}/specimens");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    [Fact]
    public async Task GetAllSpecimensInCollection_Anonymous_StaticCollection_ReturnsForbidden()
    {
        var response = await Client.GetAsync(
            $"{Routings.CommonCollectionRouting}/00000000-0000-0000-0000-000000000001/specimens");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

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
