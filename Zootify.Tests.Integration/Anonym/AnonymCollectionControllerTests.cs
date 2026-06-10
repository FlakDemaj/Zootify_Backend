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
        var result = await response.Content.ReadFromJsonAsync<List<GetCollectionResponseDto>>();
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPublicCollections_Unauthenticated_ContainsNoStaticCollections()
    {
        var response = await Client.GetAsync(Routings.AnonymCollectionRouting);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<GetCollectionResponseDto>>();
        result.Should().BeEmpty();
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

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllSpecimensInCollection_StaticCollection_ReturnsForbidden()
    {
        var response = await Client.GetAsync(
            $"{Routings.AnonymCollectionRouting}/00000000-0000-0000-0000-000000000001/specimens");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllSpecimensInCollection_DeletedPublicCollection_ReturnsForbidden()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(
            isPrivate: false,
            createdByUserGuid: owner.Guid,
            deletedByUserGuid: owner.Guid);

        var response = await Client.GetAsync($"{Routings.AnonymCollectionRouting}/{collection.Guid}/specimens");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion
}