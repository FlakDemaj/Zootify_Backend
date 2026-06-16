using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Common;

/// <summary>
/// Tests for Common Taxonomy Controller.
/// </summary>
public class CommonTaxonomyControllerTests : IntegrationTestBase
{
    public CommonTaxonomyControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    #region GetTaxonomyNodes

    /// <summary>
    /// Tests Get Taxonomy Nodes – Returns Only Species.
    /// </summary>
    [Fact]
    public async Task GetTaxonomyNodes_ReturnsOnlySpecies()
    {
        var phylum = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata", TaxonomyRank.Phylum);
        var @class = await SpecimenRepo.CreateTaxonomyNodeAsync("Mammalia", TaxonomyRank.Class, parentGuid: phylum.Guid);
        var order = await SpecimenRepo.CreateTaxonomyNodeAsync("Carnivora", TaxonomyRank.Order, parentGuid: @class.Guid);
        var family = await SpecimenRepo.CreateTaxonomyNodeAsync("Felidae", TaxonomyRank.Family, parentGuid: order.Guid);
        var genus = await SpecimenRepo.CreateTaxonomyNodeAsync("Panthera", TaxonomyRank.Genus, parentGuid: family.Guid);
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("Panthera leo", TaxonomyRank.Species, parentGuid: genus.Guid);

        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var response = await Client.GetFromJsonAsync<List<GetSpeciesForCreationDto>>(
            Routings.CommonTaxonomyRouting);

        response.Should().NotBeNull();
        // Only the species node is returned, not any of its ancestor ranks.
        response!.Should().ContainSingle();
        response.Single().TaxonomyGuid.Should().Be(species.Guid);
        response.Should().NotContain(n => n.TaxonomyGuid == genus.Guid || n.TaxonomyGuid == phylum.Guid);
    }

    /// <summary>
    /// Tests Get Taxonomy Nodes – Multiple Species – Returns All Flat.
    /// </summary>
    [Fact]
    public async Task GetTaxonomyNodes_MultipleSpecies_ReturnsAllFlat()
    {
        var genus1 = await SpecimenRepo.CreateTaxonomyNodeAsync("Panthera", TaxonomyRank.Genus);
        var species1 = await SpecimenRepo.CreateTaxonomyNodeAsync("Panthera leo", TaxonomyRank.Species, parentGuid: genus1.Guid);
        var genus2 = await SpecimenRepo.CreateTaxonomyNodeAsync("Felis", TaxonomyRank.Genus);
        var species2 = await SpecimenRepo.CreateTaxonomyNodeAsync("Felis catus", TaxonomyRank.Species, parentGuid: genus2.Guid);

        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var response = await Client.GetFromJsonAsync<List<GetSpeciesForCreationDto>>(
            Routings.CommonTaxonomyRouting);

        response.Should().NotBeNull();
        response!.Should().HaveCount(2);
        response.Should().Contain(n => n.TaxonomyGuid == species1.Guid);
        response.Should().Contain(n => n.TaxonomyGuid == species2.Guid);
    }

    /// <summary>
    /// Tests Get Taxonomy Nodes – With Filter – Returns Matching Species.
    /// </summary>
    [Fact]
    public async Task GetTaxonomyNodes_WithFilter_ReturnsMatchingSpecies()
    {
        var genus = await SpecimenRepo.CreateTaxonomyNodeAsync("Panthera", TaxonomyRank.Genus);
        await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species, parentGuid: genus.Guid);
        var matchedSpecies = await SpecimenRepo.CreateTaxonomyNodeAsync("pardus", TaxonomyRank.Species, parentGuid: genus.Guid);
        await SpecimenRepo.CreateTaxonomyNodeAsync("tigris", TaxonomyRank.Species, parentGuid: genus.Guid);

        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var response = await Client.GetFromJsonAsync<List<GetSpeciesForCreationDto>>(
            $"{Routings.CommonTaxonomyRouting}?filter=p");

        response.Should().NotBeNull();
        response!.Should().ContainSingle();
        response.Single().TaxonomyGuid.Should().Be(matchedSpecies.Guid);
    }

    /// <summary>
    /// Tests Get Taxonomy Nodes – Filter With Space – Matches Full Binomial Name.
    /// </summary>
    [Fact]
    public async Task GetTaxonomyNodes_FilterWithSpace_MatchesFullBinomialName()
    {
        var genus = await SpecimenRepo.CreateTaxonomyNodeAsync("Panthera", TaxonomyRank.Genus);
        var leo = await SpecimenRepo.CreateTaxonomyNodeAsync("Panthera leo", TaxonomyRank.Species, parentGuid: genus.Guid);
        await SpecimenRepo.CreateTaxonomyNodeAsync("Panthera tigris", TaxonomyRank.Species, parentGuid: genus.Guid);

        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        // Filter has a space in the middle and a stray trailing space → still matches "Panthera leo".
        var response = await Client.GetFromJsonAsync<List<GetSpeciesForCreationDto>>(
            $"{Routings.CommonTaxonomyRouting}?filter={Uri.EscapeDataString("Panthera leo ")}");

        response.Should().NotBeNull();
        // The trailing space is trimmed and the inner space kept; the exact match ranks first (fuzzy may
        // additionally surface the sibling "Panthera tigris" because they share the genus word).
        response!.Should().Contain(n => n.TaxonomyGuid == leo.Guid);
        response.First().TaxonomyGuid.Should().Be(leo.Guid);
    }

    /// <summary>
    /// Tests Get Taxonomy Nodes – Fuzzy Filter – Finds Species Despite Typo.
    /// </summary>
    [Fact]
    public async Task GetTaxonomyNodes_FuzzyFilter_FindsSpeciesDespiteTypo()
    {
        var genus = await SpecimenRepo.CreateTaxonomyNodeAsync("Panthera", TaxonomyRank.Genus);
        var leo = await SpecimenRepo.CreateTaxonomyNodeAsync("Panthera leo", TaxonomyRank.Species, parentGuid: genus.Guid);
        await SpecimenRepo.CreateTaxonomyNodeAsync("Carabus coriaceus", TaxonomyRank.Species, parentGuid: genus.Guid);

        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        // "leoo" is a typo of "leo" → fuzzy matching still finds "Panthera leo".
        var response = await Client.GetFromJsonAsync<List<GetSpeciesForCreationDto>>(
            $"{Routings.CommonTaxonomyRouting}?filter={Uri.EscapeDataString("Panthera leoo")}");

        response.Should().NotBeNull();
        response!.Should().Contain(n => n.TaxonomyGuid == leo.Guid);
        response.Should().NotContain(n => n.Name == "Carabus coriaceus");
    }

    /// <summary>
    /// Tests Get Taxonomy Nodes – With Filter – Returns Nothing.
    /// </summary>
    [Fact]
    public async Task GetTaxonomyNodes_WithFilter_ReturnsNothing()
    {
        var genus = await SpecimenRepo.CreateTaxonomyNodeAsync("Panthera", TaxonomyRank.Genus);
        await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species, parentGuid: genus.Guid);

        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var response = await Client.GetFromJsonAsync<List<GetSpeciesForCreationDto>>(
            $"{Routings.CommonTaxonomyRouting}?filter=xyz");

        response.Should().NotBeNull();
        response!.Should().BeEmpty();
    }

    /// <summary>
    /// Tests Get Taxonomy Nodes – Are Ordered By Name Ascending.
    /// </summary>
    [Fact]
    public async Task GetTaxonomyNodes_AreOrderedByNameAscending()
    {
        var genus = await SpecimenRepo.CreateTaxonomyNodeAsync("Panthera", TaxonomyRank.Genus);
        await SpecimenRepo.CreateTaxonomyNodeAsync("Panthera tigris", TaxonomyRank.Species, parentGuid: genus.Guid);
        await SpecimenRepo.CreateTaxonomyNodeAsync("Panthera leo", TaxonomyRank.Species, parentGuid: genus.Guid);

        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var response = await Client.GetFromJsonAsync<List<GetSpeciesForCreationDto>>(
            Routings.CommonTaxonomyRouting);

        response.Should().NotBeNull();
        response!.Should().BeInAscendingOrder(n => n.Name);
        response.Select(n => n.Name).Should().ContainInOrder("Panthera leo", "Panthera tigris");
    }

    /// <summary>
    /// Tests Get Taxonomy Nodes – Not Authenticated – Returns Unauthorized.
    /// </summary>
    [Fact]
    public async Task GetTaxonomyNodes_NotAuthenticated_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync(Routings.CommonTaxonomyRouting);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region CreateTaxonomyNode

    /// <summary>
    /// Tests Create Taxonomy Node – With No Parent – Creates Phylum Node.
    /// </summary>
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

    /// <summary>
    /// Tests Create Taxonomy Node – With Phylum Parent – Creates Class Node.
    /// </summary>
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

    /// <summary>
    /// Tests Create Taxonomy Node – Multiple Nodes In Single Request – Creates Chained Nodes.
    /// </summary>
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

    /// <summary>
    /// Tests Create Taxonomy Node – With Invalid Parent Guid – Returns No Taxonomy Found.
    /// </summary>
    [Fact]
    public async Task CreateTaxonomyNode_WithInvalidParentGuid_ReturnsNoTaxonomyFound()
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
        error.ErrorCode.Should().Be((int)ErrorCode.NoTaxonomyFound);
    }

    /// <summary>
    /// Tests Create Taxonomy Node – Not Authenticated – Returns Unauthorized.
    /// </summary>
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

    #region GetTaxonomyTree

    /// <summary>
    /// Tests Get Taxonomy Tree – Full Chain – Returns Correct Tree Structure.
    /// </summary>
    [Fact]
    public async Task GetTaxonomyTree_FullChain_ReturnsCorrectTreeStructure()
    {
        var phylum  = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata",  TaxonomyRank.Phylum);
        var @class  = await SpecimenRepo.CreateTaxonomyNodeAsync("Mammalia",  TaxonomyRank.Class,  parentGuid: phylum.Guid);
        var order   = await SpecimenRepo.CreateTaxonomyNodeAsync("Carnivora", TaxonomyRank.Order,  parentGuid: @class.Guid);
        var family  = await SpecimenRepo.CreateTaxonomyNodeAsync("Felidae",   TaxonomyRank.Family, parentGuid: order.Guid);
        var genus   = await SpecimenRepo.CreateTaxonomyNodeAsync("Panthera",  TaxonomyRank.Genus,  parentGuid: family.Guid);
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo",       TaxonomyRank.Species, parentGuid: genus.Guid);

        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var response = await Client.GetAsync(Routings.CommonTaxonomyTreeRouting);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetTaxonomyTreeResponseDto>();

        result!.TotalCount.Should().Be(1);
        var root = result.Nodes.Single();
        root.TaxonomyGuid.Should().Be(phylum.Guid);
        root.Rank.Should().Be(TaxonomyRank.Phylum);

        var level2 = root.Children.Single();
        level2.TaxonomyGuid.Should().Be(@class.Guid);
        level2.Rank.Should().Be(TaxonomyRank.Class);

        var level3 = level2.Children.Single();
        level3.TaxonomyGuid.Should().Be(order.Guid);

        var level4 = level3.Children.Single();
        level4.TaxonomyGuid.Should().Be(family.Guid);

        var level5 = level4.Children.Single();
        level5.TaxonomyGuid.Should().Be(genus.Guid);

        var level6 = level5.Children.Single();
        level6.TaxonomyGuid.Should().Be(species.Guid);
        level6.Children.Should().BeEmpty();
    }

    /// <summary>
    /// Tests Get Taxonomy Tree – Multiple Roots – Returns Both Trees.
    /// </summary>
    [Fact]
    public async Task GetTaxonomyTree_MultipleRoots_ReturnsBothTrees()
    {
        var phylum1 = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata",    TaxonomyRank.Phylum);
        var phylum2 = await SpecimenRepo.CreateTaxonomyNodeAsync("Arthropoda",  TaxonomyRank.Phylum);
        await SpecimenRepo.CreateTaxonomyNodeAsync("Mammalia",  TaxonomyRank.Class, parentGuid: phylum1.Guid);
        await SpecimenRepo.CreateTaxonomyNodeAsync("Insecta",   TaxonomyRank.Class, parentGuid: phylum2.Guid);

        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var result = await Client.GetFromJsonAsync<GetTaxonomyTreeResponseDto>(
            Routings.CommonTaxonomyTreeRouting);

        result!.TotalCount.Should().Be(2);
        result.Nodes.Should().ContainSingle(n => n.TaxonomyGuid == phylum1.Guid);
        result.Nodes.Should().ContainSingle(n => n.TaxonomyGuid == phylum2.Guid);
    }

    /// <summary>
    /// Tests Get Taxonomy Tree – Roots Are Ordered By Name Ascending.
    /// </summary>
    [Fact]
    public async Task GetTaxonomyTree_RootsAreOrderedByNameAscending()
    {
        await SpecimenRepo.CreateTaxonomyNodeAsync("Mollusca",   TaxonomyRank.Phylum);
        await SpecimenRepo.CreateTaxonomyNodeAsync("Arthropoda", TaxonomyRank.Phylum);
        await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata",   TaxonomyRank.Phylum);

        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var result = await Client.GetFromJsonAsync<GetTaxonomyTreeResponseDto>(
            Routings.CommonTaxonomyTreeRouting);

        result!.Nodes.Select(n => n.Name).Should().BeInAscendingOrder();
    }

    /// <summary>
    /// Tests Get Taxonomy Tree – Children Are Ordered By Name Ascending.
    /// </summary>
    [Fact]
    public async Task GetTaxonomyTree_ChildrenAreOrderedByNameAscending()
    {
        var phylum = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata", TaxonomyRank.Phylum);
        await SpecimenRepo.CreateTaxonomyNodeAsync("Reptilia",  TaxonomyRank.Class, parentGuid: phylum.Guid);
        await SpecimenRepo.CreateTaxonomyNodeAsync("Amphibia",  TaxonomyRank.Class, parentGuid: phylum.Guid);
        await SpecimenRepo.CreateTaxonomyNodeAsync("Mammalia",  TaxonomyRank.Class, parentGuid: phylum.Guid);

        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var result = await Client.GetFromJsonAsync<GetTaxonomyTreeResponseDto>(
            Routings.CommonTaxonomyTreeRouting);

        var root = result!.Nodes.Single();
        root.Children.Select(c => c.Name).Should().BeInAscendingOrder();
    }

    /// <summary>
    /// Tests Get Taxonomy Tree – Root With No Children – Returns Empty Children List.
    /// </summary>
    [Fact]
    public async Task GetTaxonomyTree_RootWithNoChildren_ReturnsEmptyChildrenList()
    {
        await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata", TaxonomyRank.Phylum);

        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var result = await Client.GetFromJsonAsync<GetTaxonomyTreeResponseDto>(
            Routings.CommonTaxonomyTreeRouting);

        result!.TotalCount.Should().Be(1);
        result.Nodes.Single().Children.Should().BeEmpty();
    }

    /// <summary>
    /// Tests Get Taxonomy Tree – No Nodes – Returns Empty Tree.
    /// </summary>
    [Fact]
    public async Task GetTaxonomyTree_NoNodes_ReturnsEmptyTree()
    {
        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var result = await Client.GetFromJsonAsync<GetTaxonomyTreeResponseDto>(
            Routings.CommonTaxonomyTreeRouting);

        result!.TotalCount.Should().Be(0);
        result.Nodes.Should().BeEmpty();
    }

    /// <summary>
    /// Tests Get Taxonomy Tree – Filter By Name – Returns Matching Roots Only.
    /// </summary>
    [Fact]
    public async Task GetTaxonomyTree_FilterByName_ReturnsMatchingRootsOnly()
    {
        var chordata   = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata",   TaxonomyRank.Phylum);
        var arthropoda = await SpecimenRepo.CreateTaxonomyNodeAsync("Arthropoda", TaxonomyRank.Phylum);
        await SpecimenRepo.CreateTaxonomyNodeAsync("Mammalia", TaxonomyRank.Class, parentGuid: chordata.Guid);

        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var result = await Client.GetFromJsonAsync<GetTaxonomyTreeResponseDto>(
            $"{Routings.CommonTaxonomyTreeRouting}?filterByName=Chord");

        result!.TotalCount.Should().Be(1);
        result.Nodes.Single().TaxonomyGuid.Should().Be(chordata.Guid);
        result.Nodes.Should().NotContain(n => n.TaxonomyGuid == arthropoda.Guid);
    }

    /// <summary>
    /// Tests Get Taxonomy Tree – Filter By Name – Is Case Insensitive.
    /// </summary>
    [Fact]
    public async Task GetTaxonomyTree_FilterByName_IsCaseInsensitive()
    {
        var phylum = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata", TaxonomyRank.Phylum);

        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var result = await Client.GetFromJsonAsync<GetTaxonomyTreeResponseDto>(
            $"{Routings.CommonTaxonomyTreeRouting}?filterByName=chordata");

        result!.TotalCount.Should().Be(1);
        result.Nodes.Single().TaxonomyGuid.Should().Be(phylum.Guid);
    }

    /// <summary>
    /// Tests Get Taxonomy Tree – Filter By Name – No Match – Returns Empty Tree.
    /// </summary>
    [Fact]
    public async Task GetTaxonomyTree_FilterByName_NoMatch_ReturnsEmptyTree()
    {
        await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata", TaxonomyRank.Phylum);

        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var result = await Client.GetFromJsonAsync<GetTaxonomyTreeResponseDto>(
            $"{Routings.CommonTaxonomyTreeRouting}?filterByName=zzznomatch");

        result!.TotalCount.Should().Be(0);
        result.Nodes.Should().BeEmpty();
    }

    /// <summary>
    /// Tests Get Taxonomy Tree – Filtered Root Includes Full Subtree.
    /// </summary>
    [Fact]
    public async Task GetTaxonomyTree_FilteredRootIncludesFullSubtree()
    {
        var chordata  = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata",  TaxonomyRank.Phylum);
        var mammalia  = await SpecimenRepo.CreateTaxonomyNodeAsync("Mammalia",  TaxonomyRank.Class, parentGuid: chordata.Guid);
        var carnivora = await SpecimenRepo.CreateTaxonomyNodeAsync("Carnivora", TaxonomyRank.Order, parentGuid: mammalia.Guid);
        await SpecimenRepo.CreateTaxonomyNodeAsync("Arthropoda", TaxonomyRank.Phylum);

        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var result = await Client.GetFromJsonAsync<GetTaxonomyTreeResponseDto>(
            $"{Routings.CommonTaxonomyTreeRouting}?filterByName=Chordata");

        result!.TotalCount.Should().Be(1);
        var root = result.Nodes.Single();
        root.Children.Should().HaveCount(1);
        root.Children.Single().TaxonomyGuid.Should().Be(mammalia.Guid);
        root.Children.Single().Children.Single().TaxonomyGuid.Should().Be(carnivora.Guid);
    }

    /// <summary>
    /// Tests Get Taxonomy Tree – Filter Is Prefix Match – Loads Only Trees With A Starts With Match.
    /// </summary>
    [Fact]
    public async Task GetTaxonomyTree_FilterIsPrefixMatch_LoadsOnlyTreesWithAStartsWithMatch()
    {
        // Tree 1: "Cyprinus" only contains "in" as an inner substring → the whole root must NOT be loaded.
        var chordata = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata", TaxonomyRank.Phylum);
        await SpecimenRepo.CreateTaxonomyNodeAsync("Cyprinus", TaxonomyRank.Genus, parentGuid: chordata.Guid);
        // Tree 2: only the "Insecta" branch starts with "In"; the "Arachnida" sibling must be pruned.
        var arthropoda = await SpecimenRepo.CreateTaxonomyNodeAsync("Arthropoda", TaxonomyRank.Phylum);
        var insecta = await SpecimenRepo.CreateTaxonomyNodeAsync("Insecta", TaxonomyRank.Class, parentGuid: arthropoda.Guid);
        await SpecimenRepo.CreateTaxonomyNodeAsync("Arachnida", TaxonomyRank.Class, parentGuid: arthropoda.Guid);
        // Tree 3: no match at all.
        await SpecimenRepo.CreateTaxonomyNodeAsync("Mollusca", TaxonomyRank.Phylum);

        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var result = await Client.GetFromJsonAsync<GetTaxonomyTreeResponseDto>(
            $"{Routings.CommonTaxonomyTreeRouting}?filterByName=In");

        result!.TotalCount.Should().Be(1);
        var root = result.Nodes.Single();
        root.TaxonomyGuid.Should().Be(arthropoda.Guid);
        // Only the Insecta branch remains; the non-matching Arachnida sibling is pruned out.
        root.Children.Should().ContainSingle();
        root.Children.Single().TaxonomyGuid.Should().Be(insecta.Guid);
        result.Nodes.Should().NotContain(n => n.TaxonomyGuid == chordata.Guid);
    }

    /// <summary>
    /// Tests Get Taxonomy Tree – Filter Matches Word Inside Binomial Name.
    /// </summary>
    [Fact]
    public async Task GetTaxonomyTree_FilterMatchesWordInsideBinomialName()
    {
        var chordata = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata", TaxonomyRank.Phylum);
        var panthera = await SpecimenRepo.CreateTaxonomyNodeAsync("Panthera", TaxonomyRank.Genus, parentGuid: chordata.Guid);
        var leo = await SpecimenRepo.CreateTaxonomyNodeAsync("Panthera leo", TaxonomyRank.Species, parentGuid: panthera.Guid);
        // A sibling species whose second word is not "leo" → must be pruned.
        await SpecimenRepo.CreateTaxonomyNodeAsync("Panthera tigris", TaxonomyRank.Species, parentGuid: panthera.Guid);

        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        // "leo" is the second word of "Panthera leo" → it must match even though the name starts with "Panthera".
        var result = await Client.GetFromJsonAsync<GetTaxonomyTreeResponseDto>(
            $"{Routings.CommonTaxonomyTreeRouting}?filterByName=leo");

        result!.TotalCount.Should().Be(1);
        var root = result.Nodes.Single();
        root.TaxonomyGuid.Should().Be(chordata.Guid);
        var genus = root.Children.Single();
        genus.TaxonomyGuid.Should().Be(panthera.Guid);
        genus.Children.Should().ContainSingle();
        genus.Children.Single().TaxonomyGuid.Should().Be(leo.Guid);
    }

    /// <summary>
    /// Tests Get Taxonomy Tree – Filter Matches Deep Node – Prunes Non Matching Sibling Branches.
    /// </summary>
    [Fact]
    public async Task GetTaxonomyTree_FilterMatchesDeepNode_PrunesNonMatchingSiblingBranches()
    {
        var chordata  = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata",  TaxonomyRank.Phylum);
        var mammalia  = await SpecimenRepo.CreateTaxonomyNodeAsync("Mammalia",  TaxonomyRank.Class, parentGuid: chordata.Guid);
        var carnivora = await SpecimenRepo.CreateTaxonomyNodeAsync("Carnivora", TaxonomyRank.Order, parentGuid: mammalia.Guid);
        // A sibling class with no match anywhere in its branch: it must be pruned out.
        await SpecimenRepo.CreateTaxonomyNodeAsync("Aves", TaxonomyRank.Class, parentGuid: chordata.Guid);
        // An unrelated root without any match must not be returned.
        await SpecimenRepo.CreateTaxonomyNodeAsync("Arthropoda", TaxonomyRank.Phylum);

        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        // "Carniv" matches only the Order node deep in the tree; the root does not match its own name.
        var result = await Client.GetFromJsonAsync<GetTaxonomyTreeResponseDto>(
            $"{Routings.CommonTaxonomyTreeRouting}?filterByName=Carniv");

        result!.TotalCount.Should().Be(1);
        var root = result.Nodes.Single();
        root.TaxonomyGuid.Should().Be(chordata.Guid);
        // Only the branch leading to the match is kept (Mammalia → Carnivora); the Aves sibling is pruned.
        root.Children.Should().ContainSingle();
        root.Children.Single().TaxonomyGuid.Should().Be(mammalia.Guid);
        root.Children.Single().Children.Single().TaxonomyGuid.Should().Be(carnivora.Guid);
    }

    /// <summary>
    /// Tests Get Taxonomy Tree – Node Carries Correct Approval Status.
    /// </summary>
    [Fact]
    public async Task GetTaxonomyTree_NodeCarriesCorrectApprovalStatus()
    {
        var phylum = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata", TaxonomyRank.Phylum,
            approvalStatus: ApprovalStatus.Approved);
        await SpecimenRepo.CreateTaxonomyNodeAsync("Mammalia", TaxonomyRank.Class,
            parentGuid: phylum.Guid, approvalStatus: ApprovalStatus.Pending);

        var user = await UserRepo.CreateUserAsync("test@test.de");
        AuthenticateAs(user);

        var result = await Client.GetFromJsonAsync<GetTaxonomyTreeResponseDto>(
            Routings.CommonTaxonomyTreeRouting);

        var root = result!.Nodes.Single();
        root.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
        root.Children.Single().ApprovalStatus.Should().Be(ApprovalStatus.Pending);
    }

    /// <summary>
    /// Tests Get Taxonomy Tree – Not Authenticated – Returns Unauthorized.
    /// </summary>
    [Fact]
    public async Task GetTaxonomyTree_NotAuthenticated_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync(Routings.CommonTaxonomyTreeRouting);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Helpers

    #endregion
}
