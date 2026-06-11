using Zootify_Backend.DTOs.Common.Response;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Anonym;

public class AnonymCollectionControllerTests : IntegrationTestBase
{
    public AnonymCollectionControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    #region GetPublicCollections

    [Fact]
    public async Task GetPublicCollections_AllowsUnauthenticatedAccess()
    {
        await CollectionRepo.CreateCollectionAsync(name: "Public", isPrivate: false);

        var response = await Client.GetAsync(Routings.AnonymCollectionRouting);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        result!.TotalCount.Should().Be(1);
        result.Collections.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPublicCollections_Unauthenticated_ContainsNoStaticCollections()
    {
        var response = await Client.GetAsync(Routings.AnonymCollectionRouting);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        result!.TotalCount.Should().Be(0);
        result.Collections.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPublicCollections_PrivateCollectionIsNotReturned()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        await CollectionRepo.CreateCollectionAsync(name: "Secret", isPrivate: true, createdByUserGuid: owner.Guid);
        await CollectionRepo.CreateCollectionAsync(name: "Open", isPrivate: false, createdByUserGuid: owner.Guid);

        var response = await Client.GetAsync(Routings.AnonymCollectionRouting);

        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        result!.TotalCount.Should().Be(1);
        result.Collections.Should().Contain(c => c.Name == "Open");
        result.Collections.Should().NotContain(c => c.Name == "Secret");
    }

    [Fact]
    public async Task GetPublicCollections_Paging_ReturnsTotalCountAndCorrectPage()
    {
        for (var i = 0; i < 5; i++)
        {
            await CollectionRepo.CreateCollectionAsync(name: $"Col {i}", isPrivate: false);
        }

        var response = await Client.GetAsync(
            $"{Routings.AnonymCollectionRouting}?page=1&pageSize=3");

        var result = await response.Content.ReadFromJsonAsync<GetAllCollectionsPagedResponseDto>();
        result!.TotalCount.Should().Be(5);
        result.Collections.Should().HaveCount(3);
    }

    #endregion

    #region GetAllSpecimensInCollection

    [Fact]
    public async Task GetAllSpecimensInCollection_PublicCollection_OnlyPublicSpecimensReturned()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var collection = await CollectionRepo.CreateCollectionAsync(isPrivate: false, createdByUserGuid: owner.Guid);
        var publicSp = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true, createdByUserGuid: owner.Guid);
        var privateSp = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: false, createdByUserGuid: owner.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, publicSp.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, privateSp.Guid);

        var response = await Client.GetAsync($"{Routings.AnonymCollectionRouting}/{collection.Guid}/specimens");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetAllSpecimenInCollectionResponseDto>();
        result!.TotalCount.Should().Be(1);
        result.Specimens.Single().SpecimenGuid.Should().Be(publicSp.Guid);
    }

    [Fact]
    public async Task GetAllSpecimensInCollection_PrivateCollection_ReturnsForbidden()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(isPrivate: true, createdByUserGuid: owner.Guid);

        var response = await Client.GetAsync($"{Routings.AnonymCollectionRouting}/{collection.Guid}/specimens");

        var error = await ReadErrorAsync(response);
        
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);    }

    [Fact]
    public async Task GetAllSpecimensInCollection_StaticCollection_ReturnsForbidden()
    {
        var response = await Client.GetAsync(
            $"{Routings.AnonymCollectionRouting}/00000000-0000-0000-0000-000000000001/specimens");

        var error = await ReadErrorAsync(response);
        
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);    }

    [Fact]
    public async Task GetAllSpecimensInCollection_DeletedPublicCollection_ReturnsForbidden()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(
            isPrivate: false,
            createdByUserGuid: owner.Guid,
            deletedByUserGuid: owner.Guid);

        var response = await Client.GetAsync($"{Routings.AnonymCollectionRouting}/{collection.Guid}/specimens");

        var error = await ReadErrorAsync(response);
        
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);    }

    #endregion
}