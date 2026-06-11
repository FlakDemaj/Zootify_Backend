using Zootify_Backend.DTOs.Common.Response;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Anonym;

public class AnonymSpecimenControllerTests : IntegrationTestBase
{
    private static readonly Guid AllSpecimensGuid = new("00000000-0000-0000-0000-000000000001");
    private static readonly Guid MyCollectionGuid = new("00000000-0000-0000-0000-000000000002");

    public AnonymSpecimenControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    #region GetSpecimenDetail

    [Fact]
    public async Task GetSpecimenDetail_PublicSpecimen_ReturnsDetail()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var collection = await CollectionRepo.CreateCollectionAsync(isPrivate: false, createdByUserGuid: owner.Guid);
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true, createdByUserGuid: owner.Guid);
        await CollectionRepo.CreateSpecimenCollectionAsync(collection.Guid, specimen.Guid);

        var response = await Client.GetAsync(
            $"{Routings.AnonymSpecimenRouting}/{specimen.Guid}?collectionGuid={collection.Guid}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetSpecimenDetailResponseDto>();
        result!.SpecimenGuid.Should().Be(specimen.Guid);
        result.IsPublic.Should().BeTrue();
    }

    [Fact]
    public async Task GetSpecimenDetail_PrivateSpecimen_ReturnsForbidden()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: false, createdByUserGuid: owner.Guid);

        var response = await Client.GetAsync(
            $"{Routings.AnonymSpecimenRouting}/{specimen.Guid}?collectionGuid={AllSpecimensGuid}");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    [Fact]
    public async Task GetSpecimenDetail_SpecimenNotInCollection_ReturnsSpecimenNotInCollection()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var collection = await CollectionRepo.CreateCollectionAsync(isPrivate: false, createdByUserGuid: owner.Guid);
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true, createdByUserGuid: owner.Guid);

        var response = await Client.GetAsync(
            $"{Routings.AnonymSpecimenRouting}/{specimen.Guid}?collectionGuid={collection.Guid}");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.SpecimenNotInCollection);
    }

    [Fact]
    public async Task GetSpecimenDetail_StaticMyCollectionGuid_ReturnsForbidden()
    {
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, isPublic: true, createdByUserGuid: owner.Guid);

        var response = await Client.GetAsync(
            $"{Routings.AnonymSpecimenRouting}/{specimen.Guid}?collectionGuid={MyCollectionGuid}");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.SpecimenNotInCollection);
    }

    [Fact]
    public async Task GetSpecimenDetail_UnknownSpecimenGuid_ReturnsNoSpecimenWithThisGuid()
    {
        var response = await Client.GetAsync(
            $"{Routings.AnonymSpecimenRouting}/{Guid.CreateVersion7()}?collectionGuid={AllSpecimensGuid}");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoSpecimenWithThisGuid);
    }

    [Fact]
    public async Task GetSpecimenDetail_TaxonomyPathPresentForPublicSpecimen()
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
            $"{Routings.AnonymSpecimenRouting}/{specimen.Guid}?collectionGuid={collection.Guid}");

        result!.TaxonomyPath.Should().HaveCount(3);
        result.TaxonomyPath.Should().BeInAscendingOrder(n => n.Rank);
    }

    #endregion
}
