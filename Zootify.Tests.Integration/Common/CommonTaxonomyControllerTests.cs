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

        var request = new CreateTaxonomyNodeRequestDto
        {
            TaxonomyNodes = new Dictionary<TaxonomyRank, string> { { TaxonomyRank.Phylum, "Chordata" } }
        };

        var response = await Client.PostAsJsonAsync(Routings.CommonTaxonomyRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<CreatedObjectResponseDto>>();
        result.Should().NotBeNull();
        result!.Should().HaveCount(1);

        var node = await GetTaxonomyNodeFromDbAsync(result[0].CreatedObjectGuid);
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
            TaxonomyNodes = new Dictionary<TaxonomyRank, string> { { TaxonomyRank.Class, "Mammalia" } },
            ParentGuid = phylum.Guid
        };

        var response = await Client.PostAsJsonAsync(Routings.CommonTaxonomyRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<CreatedObjectResponseDto>>();
        result.Should().NotBeNull();
        result!.Should().HaveCount(1);

        var node = await GetTaxonomyNodeFromDbAsync(result[0].CreatedObjectGuid);

        node.Should().NotBeNull();
        node!.Rank.Should().Be(TaxonomyRank.Class);
        node.ParentGuid.Should().Be(phylum.Guid);
        node.CreatedByUserGuid.Should().Be(user.Guid);
    }

    [Fact]
    public async Task CreateTaxonomyNode_MultipleNodesInSingleRequest_CreatesChainedNodes()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var request = new CreateTaxonomyNodeRequestDto
        {
            TaxonomyNodes = new Dictionary<TaxonomyRank, string>
            {
                { TaxonomyRank.Phylum, "Chordata" },
                { TaxonomyRank.Class, "Mammalia" },
                { TaxonomyRank.Order, "Carnivora" },
                { TaxonomyRank.Family, "Felidae" },
                { TaxonomyRank.Genus, "Panthera" },
                { TaxonomyRank.Species, "leo" }
            }
        };

        var response = await Client.PostAsJsonAsync(Routings.CommonTaxonomyRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<CreatedObjectResponseDto>>();
        result.Should().NotBeNull();
        result!.Should().HaveCount(6);

        var phylum = await GetTaxonomyNodeFromDbAsync(result[0].CreatedObjectGuid);
        var @class = await GetTaxonomyNodeFromDbAsync(result[1].CreatedObjectGuid);
        var order = await GetTaxonomyNodeFromDbAsync(result[2].CreatedObjectGuid);
        var family = await GetTaxonomyNodeFromDbAsync(result[3].CreatedObjectGuid);
        var genus = await GetTaxonomyNodeFromDbAsync(result[4].CreatedObjectGuid);
        var species = await GetTaxonomyNodeFromDbAsync(result[5].CreatedObjectGuid);

        phylum!.Rank.Should().Be(TaxonomyRank.Phylum);
        phylum.ParentGuid.Should().BeNull();

        @class!.Rank.Should().Be(TaxonomyRank.Class);
        @class.ParentGuid.Should().Be(result[0].CreatedObjectGuid);

        order!.Rank.Should().Be(TaxonomyRank.Order);
        order.ParentGuid.Should().Be(result[1].CreatedObjectGuid);

        family!.Rank.Should().Be(TaxonomyRank.Family);
        family.ParentGuid.Should().Be(result[2].CreatedObjectGuid);

        genus!.Rank.Should().Be(TaxonomyRank.Genus);
        genus.ParentGuid.Should().Be(result[3].CreatedObjectGuid);

        species!.Rank.Should().Be(TaxonomyRank.Species);
        species.ParentGuid.Should().Be(result[4].CreatedObjectGuid);
    }

    [Fact]
    public async Task CreateTaxonomyNode_WithInvalidParentGuid_ReturnsNoTaxonomyWithThisGuid()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var request = new CreateTaxonomyNodeRequestDto
        {
            TaxonomyNodes = new Dictionary<TaxonomyRank, string> { { TaxonomyRank.Class, "Mammalia" } },
            ParentGuid = Guid.CreateVersion7()
        };

        var response = await Client.PostAsJsonAsync(Routings.CommonTaxonomyRouting, request);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoTaxonomyWithThisGuid);
    }

    [Fact]
    public async Task CreateTaxonomyNode_NotAuthenticated_ReturnsUnauthorized()
    {
        var request = new CreateTaxonomyNodeRequestDto
        {
            TaxonomyNodes = new Dictionary<TaxonomyRank, string> { { TaxonomyRank.Phylum, "Chordata" } }
        };

        var response = await Client.PostAsJsonAsync(Routings.CommonTaxonomyRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Helpers

    #endregion
}
