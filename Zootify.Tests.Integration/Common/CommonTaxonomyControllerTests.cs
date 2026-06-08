using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Common;

public class CommonTaxonomyControllerTests : IntegrationTestBase
{
    public CommonTaxonomyControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    #region GetTaxonomyNodes

    [Fact]
    public async Task GetTaxonomyNodes_ByRank_ReturnsMatchingNodesWithTree()
    {
        var phylum = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata", TaxonomyRank.Phylum);
        var @class = await SpecimenRepo.CreateTaxonomyNodeAsync("Mammalia", TaxonomyRank.Class, parentGuid: phylum.Guid);
        var order = await SpecimenRepo.CreateTaxonomyNodeAsync("Carnivora", TaxonomyRank.Order, parentGuid: @class.Guid);
        var family = await SpecimenRepo.CreateTaxonomyNodeAsync("Felidae", TaxonomyRank.Family, parentGuid: order.Guid);
        var genus = await SpecimenRepo.CreateTaxonomyNodeAsync("Panthera", TaxonomyRank.Genus, parentGuid: family.Guid);
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species, parentGuid: genus.Guid);

        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var response = await Client.GetFromJsonAsync<List<List<GetAllTaxonomyNodesByFilterDto>>>(
            $"{Routings.CommonTaxonomyRouting}?rank={(int)TaxonomyRank.Species}");

        response.Should().NotBeNull();
        response!.Should().HaveCount(1);

        var tree = response![0];
        tree.Should().HaveCount(6);
        tree.Should().ContainSingle(n => n.TaxonomyGuid == species.Guid && n.Rank == TaxonomyRank.Species);
        tree.Should().ContainSingle(n => n.TaxonomyGuid == genus.Guid && n.Rank == TaxonomyRank.Genus);
        tree.Should().ContainSingle(n => n.TaxonomyGuid == phylum.Guid && n.Rank == TaxonomyRank.Phylum);
    }

    [Fact]
    public async Task GetTaxonomyNodes_MultipleNodesWithSameRank_ReturnsOneTreePerNode()
    {
        var genus1 = await SpecimenRepo.CreateTaxonomyNodeAsync("Panthera", TaxonomyRank.Genus);
        var species1 = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species, parentGuid: genus1.Guid);
        var genus2 = await SpecimenRepo.CreateTaxonomyNodeAsync("Felis", TaxonomyRank.Genus);
        var species2 = await SpecimenRepo.CreateTaxonomyNodeAsync("catus", TaxonomyRank.Species, parentGuid: genus2.Guid);

        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var response = await Client.GetFromJsonAsync<List<List<GetAllTaxonomyNodesByFilterDto>>>(
            $"{Routings.CommonTaxonomyRouting}?rank={(int)TaxonomyRank.Species}");

        response.Should().NotBeNull();
        response!.Should().HaveCount(2);

        response.Should().ContainSingle(tree =>
            tree.Any(n => n.TaxonomyGuid == species1.Guid));
        response.Should().ContainSingle(tree =>
            tree.Any(n => n.TaxonomyGuid == species2.Guid));
    }

    [Fact]
    public async Task GetTaxonomyNodes_WithStartsWithFilter_ReturnsMatchingNodes()
    {
        var genus = await SpecimenRepo.CreateTaxonomyNodeAsync("Panthera", TaxonomyRank.Genus);
        await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species, parentGuid: genus.Guid);
        var matchedSpecies = await SpecimenRepo.CreateTaxonomyNodeAsync("pardus", TaxonomyRank.Species, parentGuid: genus.Guid);
        await SpecimenRepo.CreateTaxonomyNodeAsync("tigris", TaxonomyRank.Species, parentGuid: genus.Guid);

        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var response = await Client.GetFromJsonAsync<List<List<GetAllTaxonomyNodesByFilterDto>>>(
            $"{Routings.CommonTaxonomyRouting}?startsWith=p&rank={(int)TaxonomyRank.Species}");

        response.Should().NotBeNull();
        response!.Should().HaveCount(1);
        response![0].Should().ContainSingle(n => n.TaxonomyGuid == matchedSpecies.Guid);
    }

    [Fact]
    public async Task GetTaxonomyNodes_WithStartsWithFilter_ReturnsNothing()
    {
        var genus = await SpecimenRepo.CreateTaxonomyNodeAsync("Panthera", TaxonomyRank.Genus);
        await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species, parentGuid: genus.Guid);

        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var response = await Client.GetFromJsonAsync<List<List<GetAllTaxonomyNodesByFilterDto>>>(
            $"{Routings.CommonTaxonomyRouting}?startsWith=xyz&rank={(int)TaxonomyRank.Species}");

        response.Should().NotBeNull();
        response!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTaxonomyNodes_TreeIsOrderedByRankAscending()
    {
        var phylum = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata", TaxonomyRank.Phylum);
        var @class = await SpecimenRepo.CreateTaxonomyNodeAsync("Mammalia", TaxonomyRank.Class, parentGuid: phylum.Guid);
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("sapiens", TaxonomyRank.Species, parentGuid: @class.Guid);

        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var response = await Client.GetFromJsonAsync<List<List<GetAllTaxonomyNodesByFilterDto>>>(
            $"{Routings.CommonTaxonomyRouting}?rank={(int)TaxonomyRank.Species}");

        response.Should().NotBeNull();
        var tree = response![0];

        tree.Should().BeInAscendingOrder(n => n.Rank);
        tree[0].TaxonomyGuid.Should().Be(phylum.Guid);
        tree[^1].TaxonomyGuid.Should().Be(species.Guid);
    }

    [Fact]
    public async Task GetTaxonomyNodes_NotAuthenticated_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync(
            $"{Routings.CommonTaxonomyRouting}?rank={(int)TaxonomyRank.Species}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region CreateTaxonomyNode

    [Fact]
    public async Task CreateTaxonomyNode_WithNoParent_CreatesPhylumNode()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var request = new CreateTaxonomyNodeRequestDto { TaxonomyName = "Chordata" };

        var response = await Client.PostAsJsonAsync(Routings.CommonTaxonomyRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CreatedObjectResponseDto>();
        result.Should().NotBeNull();

        var node = await GetTaxonomyNodeFromDbAsync(result!.CreatedObjectGuid);
        node.Should().NotBeNull();
        node!.Name.Should().Be("Chordata");
        node.Rank.Should().Be(TaxonomyRank.Phylum);
        node.ParentGuid.Should().BeNull();
        node.CreatedByUserGuid.Should().Be(user.Guid);
    }

    [Fact]
    public async Task CreateTaxonomyNode_WithPhylumParent_CreatesClassNode()
    {
        var phylum = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata", TaxonomyRank.Phylum);

        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var request = new CreateTaxonomyNodeRequestDto
        {
            TaxonomyName = "Mammalia",
            ParentGuid = phylum.Guid
        };

        var response = await Client.PostAsJsonAsync(Routings.CommonTaxonomyRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CreatedObjectResponseDto>();
        var node = await GetTaxonomyNodeFromDbAsync(result!.CreatedObjectGuid);

        node.Should().NotBeNull();
        node!.Rank.Should().Be(TaxonomyRank.Class);
        node.ParentGuid.Should().Be(phylum.Guid);
        node.CreatedByUserGuid.Should().Be(user.Guid);
    }

    [Fact]
    public async Task CreateTaxonomyNode_EachRankFollowsFromParent()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var phylum = await CreateNodeViaApiAsync(null, "Chordata");
        var @class = await CreateNodeViaApiAsync(phylum, "Mammalia");
        var order = await CreateNodeViaApiAsync(@class, "Carnivora");
        var family = await CreateNodeViaApiAsync(order, "Felidae");
        var genus = await CreateNodeViaApiAsync(family, "Panthera");
        var species = await CreateNodeViaApiAsync(genus, "leo");

        (await GetTaxonomyNodeFromDbAsync(phylum))!.Rank.Should().Be(TaxonomyRank.Phylum);
        (await GetTaxonomyNodeFromDbAsync(@class))!.Rank.Should().Be(TaxonomyRank.Class);
        (await GetTaxonomyNodeFromDbAsync(order))!.Rank.Should().Be(TaxonomyRank.Order);
        (await GetTaxonomyNodeFromDbAsync(family))!.Rank.Should().Be(TaxonomyRank.Family);
        (await GetTaxonomyNodeFromDbAsync(genus))!.Rank.Should().Be(TaxonomyRank.Genus);
        (await GetTaxonomyNodeFromDbAsync(species))!.Rank.Should().Be(TaxonomyRank.Species);
    }

    [Fact]
    public async Task CreateTaxonomyNode_WithSpeciesParent_ReturnsTaxonomyRankAtDeepestLevel()
    {
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);

        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var request = new CreateTaxonomyNodeRequestDto
        {
            TaxonomyName = "sub-leo",
            ParentGuid = species.Guid
        };

        var response = await Client.PostAsJsonAsync(Routings.CommonTaxonomyRouting, request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.TaxonomyRankIsAtTheDeepestLevel);
    }

    [Fact]
    public async Task CreateTaxonomyNode_WithInvalidParentGuid_ReturnsNoTaxonomyWithThisGuid()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var request = new CreateTaxonomyNodeRequestDto
        {
            TaxonomyName = "Mammalia",
            ParentGuid = Guid.CreateVersion7()
        };

        var response = await Client.PostAsJsonAsync(Routings.CommonTaxonomyRouting, request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoTaxonomyWithThisGuid);
    }

    [Fact]
    public async Task CreateTaxonomyNode_NotAuthenticated_ReturnsUnauthorized()
    {
        var request = new CreateTaxonomyNodeRequestDto { TaxonomyName = "Chordata" };

        var response = await Client.PostAsJsonAsync(Routings.CommonTaxonomyRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Helpers

    private async Task<Guid> CreateNodeViaApiAsync(Guid? parentGuid, string name)
    {
        var request = new CreateTaxonomyNodeRequestDto
        {
            TaxonomyName = name,
            ParentGuid = parentGuid
        };
        var response = await Client.PostAsJsonAsync(Routings.CommonTaxonomyRouting, request);
        var result = await response.Content.ReadFromJsonAsync<CreatedObjectResponseDto>();
        return result!.CreatedObjectGuid;
    }

    #endregion
}
