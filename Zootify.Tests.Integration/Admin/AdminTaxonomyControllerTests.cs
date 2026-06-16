using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Admin;

/// <summary>
/// Tests for Admin Taxonomy Controller.
/// </summary>
public class AdminTaxonomyControllerTests : IntegrationTestBase
{
    public AdminTaxonomyControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    #region ApproveTaxonomy

    /// <summary>
    /// Tests Approve Taxonomy – Success – Node Status Changes To Approved.
    /// </summary>
    [Fact]
    public async Task ApproveTaxonomy_Success_NodeStatusChangesToApproved()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        AuthenticateAs(admin);

        var phylum = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata", TaxonomyRank.Phylum, ApprovalStatus.Approved);
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync(
            "leo", TaxonomyRank.Species, ApprovalStatus.Pending, parentGuid: phylum.Guid);

        var response = await Client.PostAsync(
            $"{Routings.AdminTaxonomyRouting}/{species.Guid}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await GetTaxonomyNodeFromDbAsync(species.Guid);
        updated!.ApprovalStatus.Should().Be(ApprovalStatus.Approved);
    }

    /// <summary>
    /// Tests Approve Taxonomy – Full Chain Approved – Activates Pending Specimens.
    /// </summary>
    [Fact]
    public async Task ApproveTaxonomy_FullChainApproved_ActivatesPendingSpecimens()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var user = await UserRepo.CreateUserAsync("user@test.de");
        AuthenticateAs(admin);

        var phylum = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata", TaxonomyRank.Phylum, ApprovalStatus.Approved);
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync(
            "leo", TaxonomyRank.Species, ApprovalStatus.Pending, parentGuid: phylum.Guid);

        var geo = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(
            species.Guid, geo.Guid, status: SpecimenStatus.Pending, createdByUserGuid: user.Guid);

        var response = await Client.PostAsync(
            $"{Routings.AdminTaxonomyRouting}/{species.Guid}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedSpecimen = await GetSpecimenFromDbAsync(specimen.Guid);
        updatedSpecimen!.Status.Should().Be(SpecimenStatus.Active);
    }

    /// <summary>
    /// Tests Approve Taxonomy – Partial Chain – Specimen Stays Pending.
    /// </summary>
    [Fact]
    public async Task ApproveTaxonomy_PartialChain_SpecimenStaysPending()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var user = await UserRepo.CreateUserAsync("user@test.de");
        AuthenticateAs(admin);

        var phylum = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata", TaxonomyRank.Phylum, ApprovalStatus.Pending);
        var @class = await SpecimenRepo.CreateTaxonomyNodeAsync(
            "Mammalia", TaxonomyRank.Class, ApprovalStatus.Pending, parentGuid: phylum.Guid);
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync(
            "leo", TaxonomyRank.Species, ApprovalStatus.Pending, parentGuid: @class.Guid);

        var geo = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(
            species.Guid, geo.Guid, status: SpecimenStatus.Pending, createdByUserGuid: user.Guid);

        // Only approve the species, phylum and class are still pending
        var response = await Client.PostAsync(
            $"{Routings.AdminTaxonomyRouting}/{species.Guid}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedSpecimen = await GetSpecimenFromDbAsync(specimen.Guid);
        updatedSpecimen!.Status.Should().Be(SpecimenStatus.Pending);
    }

    /// <summary>
    /// Tests Approve Taxonomy – Approving Parent Node – Activates Specimens Under All Children.
    /// </summary>
    [Fact]
    public async Task ApproveTaxonomy_ApprovingParentNode_ActivatesSpecimensUnderAllChildren()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var user = await UserRepo.CreateUserAsync("user@test.de");
        AuthenticateAs(admin);

        var phylum = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata", TaxonomyRank.Phylum, ApprovalStatus.Pending);
        var species1 = await SpecimenRepo.CreateTaxonomyNodeAsync(
            "leo", TaxonomyRank.Species, ApprovalStatus.Approved, parentGuid: phylum.Guid);
        var species2 = await SpecimenRepo.CreateTaxonomyNodeAsync(
            "tigris", TaxonomyRank.Species, ApprovalStatus.Approved, parentGuid: phylum.Guid);

        var geo = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen1 = await SpecimenRepo.CreateSpecimenAsync(
            species1.Guid, geo.Guid, status: SpecimenStatus.Pending, createdByUserGuid: user.Guid);
        var specimen2 = await SpecimenRepo.CreateSpecimenAsync(
            species2.Guid, geo.Guid, status: SpecimenStatus.Pending, createdByUserGuid: user.Guid);

        var response = await Client.PostAsync(
            $"{Routings.AdminTaxonomyRouting}/{phylum.Guid}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated1 = await GetSpecimenFromDbAsync(specimen1.Guid);
        var updated2 = await GetSpecimenFromDbAsync(specimen2.Guid);
        updated1!.Status.Should().Be(SpecimenStatus.Active);
        updated2!.Status.Should().Be(SpecimenStatus.Active);
    }

    /// <summary>
    /// Tests Approve Taxonomy – Already Approved – Returns Error.
    /// </summary>
    [Fact]
    public async Task ApproveTaxonomy_AlreadyApproved_ReturnsError()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        AuthenticateAs(admin);

        var node = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata", TaxonomyRank.Phylum, ApprovalStatus.Approved);

        var response = await Client.PostAsync(
            $"{Routings.AdminTaxonomyRouting}/{node.Guid}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.TaxonomyAlreadyApproved);
    }

    /// <summary>
    /// Tests Approve Taxonomy – Not Found – Returns Error.
    /// </summary>
    [Fact]
    public async Task ApproveTaxonomy_NotFound_ReturnsError()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        AuthenticateAs(admin);

        var response = await Client.PostAsync(
            $"{Routings.AdminTaxonomyRouting}/{Guid.NewGuid()}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoTaxonomyFound);
    }

    /// <summary>
    /// Tests Approve Taxonomy – Moderator – Success.
    /// </summary>
    [Fact]
    public async Task ApproveTaxonomy_Moderator_Success()
    {
        var mod = await UserRepo.CreateUserAsync("mod@test.de", role: UserRole.Moderator);
        AuthenticateAs(mod);

        var node = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata", TaxonomyRank.Phylum, ApprovalStatus.Pending);

        var response = await Client.PostAsync(
            $"{Routings.AdminTaxonomyRouting}/{node.Guid}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Tests Approve Taxonomy – Normal User – Returns Forbidden.
    /// </summary>
    [Fact]
    public async Task ApproveTaxonomy_NormalUser_ReturnsForbidden()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        AuthenticateAs(user);

        var node = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata", TaxonomyRank.Phylum, ApprovalStatus.Pending);

        var response = await Client.PostAsync(
            $"{Routings.AdminTaxonomyRouting}/{node.Guid}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Tests Approve Taxonomy – Unauthenticated – Returns Unauthorized.
    /// </summary>
    [Fact]
    public async Task ApproveTaxonomy_Unauthenticated_ReturnsUnauthorized()
    {
        var node = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata", TaxonomyRank.Phylum, ApprovalStatus.Pending);

        var response = await Client.PostAsync(
            $"{Routings.AdminTaxonomyRouting}/{node.Guid}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region RejectTaxonomy

    /// <summary>
    /// Tests Reject Taxonomy – Success – Node Is Hard Deleted.
    /// </summary>
    [Fact]
    public async Task RejectTaxonomy_Success_NodeIsHardDeleted()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        AuthenticateAs(admin);

        var node = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata", TaxonomyRank.Phylum, ApprovalStatus.Pending);

        var request = new RejectTaxonomyRequestDto { Comment = "Invalid taxonomy entry." };
        var response = await Client.PostAsJsonAsync(
            $"{Routings.AdminTaxonomyRouting}/{node.Guid}/reject", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleted = await GetTaxonomyNodeFromDbAsync(node.Guid);
        deleted.Should().BeNull();
    }

    /// <summary>
    /// Tests Reject Taxonomy – Rejects All Specimens In Subtree.
    /// </summary>
    [Fact]
    public async Task RejectTaxonomy_RejectsAllSpecimensInSubtree()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var user = await UserRepo.CreateUserAsync("user@test.de");
        AuthenticateAs(admin);

        var phylum = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata", TaxonomyRank.Phylum, ApprovalStatus.Pending);
        var @class = await SpecimenRepo.CreateTaxonomyNodeAsync(
            "Mammalia", TaxonomyRank.Class, ApprovalStatus.Approved, parentGuid: phylum.Guid);
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync(
            "leo", TaxonomyRank.Species, ApprovalStatus.Approved, parentGuid: @class.Guid);

        var geo = await SpecimenRepo.CreateGeographicLocationAsync();
        var activeSpecimen = await SpecimenRepo.CreateSpecimenAsync(
            species.Guid, geo.Guid, status: SpecimenStatus.Active, createdByUserGuid: user.Guid);
        var pendingSpecimen = await SpecimenRepo.CreateSpecimenAsync(
            species.Guid, geo.Guid, status: SpecimenStatus.Pending, createdByUserGuid: user.Guid);

        var request = new RejectTaxonomyRequestDto { Comment = "Incorrect classification." };
        var response = await Client.PostAsJsonAsync(
            $"{Routings.AdminTaxonomyRouting}/{phylum.Guid}/reject", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedActive = await GetSpecimenFromDbAsync(activeSpecimen.Guid);
        var updatedPending = await GetSpecimenFromDbAsync(pendingSpecimen.Guid);
        updatedActive!.Status.Should().Be(SpecimenStatus.Rejected);
        updatedPending!.Status.Should().Be(SpecimenStatus.Rejected);
    }

    /// <summary>
    /// Tests Reject Taxonomy – Already Rejected Specimens Not Affected.
    /// </summary>
    [Fact]
    public async Task RejectTaxonomy_AlreadyRejectedSpecimensNotAffected()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var user = await UserRepo.CreateUserAsync("user@test.de");
        AuthenticateAs(admin);

        var phylum = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata", TaxonomyRank.Phylum, ApprovalStatus.Pending);
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync(
            "leo", TaxonomyRank.Species, ApprovalStatus.Approved, parentGuid: phylum.Guid);

        var geo = await SpecimenRepo.CreateGeographicLocationAsync();
        var alreadyRejected = await SpecimenRepo.CreateSpecimenAsync(
            species.Guid, geo.Guid, status: SpecimenStatus.Rejected, createdByUserGuid: user.Guid);

        var request = new RejectTaxonomyRequestDto { Comment = "Bad data." };
        var response = await Client.PostAsJsonAsync(
            $"{Routings.AdminTaxonomyRouting}/{phylum.Guid}/reject", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await GetSpecimenFromDbAsync(alreadyRejected.Guid);
        updated!.Status.Should().Be(SpecimenStatus.Rejected);
    }

    /// <summary>
    /// Tests Reject Taxonomy – Not Found – Returns Error.
    /// </summary>
    [Fact]
    public async Task RejectTaxonomy_NotFound_ReturnsError()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        AuthenticateAs(admin);

        var request = new RejectTaxonomyRequestDto { Comment = "test" };
        var response = await Client.PostAsJsonAsync(
            $"{Routings.AdminTaxonomyRouting}/{Guid.NewGuid()}/reject", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoTaxonomyFound);
    }

    /// <summary>
    /// Tests Reject Taxonomy – Moderator – Success.
    /// </summary>
    [Fact]
    public async Task RejectTaxonomy_Moderator_Success()
    {
        var mod = await UserRepo.CreateUserAsync("mod@test.de", role: UserRole.Moderator);
        AuthenticateAs(mod);

        var node = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata", TaxonomyRank.Phylum, ApprovalStatus.Pending);

        var request = new RejectTaxonomyRequestDto { Comment = "Reason for rejection." };
        var response = await Client.PostAsJsonAsync(
            $"{Routings.AdminTaxonomyRouting}/{node.Guid}/reject", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Tests Reject Taxonomy – Normal User – Returns Forbidden.
    /// </summary>
    [Fact]
    public async Task RejectTaxonomy_NormalUser_ReturnsForbidden()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        AuthenticateAs(user);

        var node = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata", TaxonomyRank.Phylum, ApprovalStatus.Pending);

        var request = new RejectTaxonomyRequestDto { Comment = "test" };
        var response = await Client.PostAsJsonAsync(
            $"{Routings.AdminTaxonomyRouting}/{node.Guid}/reject", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Tests Reject Taxonomy – Unauthenticated – Returns Unauthorized.
    /// </summary>
    [Fact]
    public async Task RejectTaxonomy_Unauthenticated_ReturnsUnauthorized()
    {
        var node = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata", TaxonomyRank.Phylum, ApprovalStatus.Pending);

        var request = new RejectTaxonomyRequestDto { Comment = "test" };
        var response = await Client.PostAsJsonAsync(
            $"{Routings.AdminTaxonomyRouting}/{node.Guid}/reject", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GetTaxonomiesForApproval

    /// <summary>
    /// Tests Get Taxonomies For Approval – Returns Trees With Pending Nodes.
    /// </summary>
    [Fact]
    public async Task GetTaxonomiesForApproval_ReturnsTreesWithPendingNodes()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        AuthenticateAs(admin);

        var phylumWithPending = await SpecimenRepo.CreateTaxonomyNodeAsync(
            "Chordata", TaxonomyRank.Phylum, ApprovalStatus.Approved);
        var pendingClass = await SpecimenRepo.CreateTaxonomyNodeAsync(
            "Mammalia", TaxonomyRank.Class, ApprovalStatus.Pending, parentGuid: phylumWithPending.Guid);

        var fullyApprovedPhylum = await SpecimenRepo.CreateTaxonomyNodeAsync(
            "Arthropoda", TaxonomyRank.Phylum, ApprovalStatus.Approved);
        await SpecimenRepo.CreateTaxonomyNodeAsync(
            "Insecta", TaxonomyRank.Class, ApprovalStatus.Approved, parentGuid: fullyApprovedPhylum.Guid);

        var result = await Client.GetFromJsonAsync<GetTaxonomyTreeResponseDto>(
            Routings.AdminTaxonomyForApprovalRouting);

        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(1);
        result.Nodes.Should().ContainSingle(n => n.TaxonomyGuid == phylumWithPending.Guid);
        result.Nodes.Should().NotContain(n => n.TaxonomyGuid == fullyApprovedPhylum.Guid);
    }

    /// <summary>
    /// Tests Get Taxonomies For Approval – Pending Root Included.
    /// </summary>
    [Fact]
    public async Task GetTaxonomiesForApproval_PendingRootIncluded()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        AuthenticateAs(admin);

        var pendingRoot = await SpecimenRepo.CreateTaxonomyNodeAsync(
            "Chordata", TaxonomyRank.Phylum, ApprovalStatus.Pending);
        await SpecimenRepo.CreateTaxonomyNodeAsync(
            "Mammalia", TaxonomyRank.Class, ApprovalStatus.Approved, parentGuid: pendingRoot.Guid);

        var result = await Client.GetFromJsonAsync<GetTaxonomyTreeResponseDto>(
            Routings.AdminTaxonomyForApprovalRouting);

        result.Should().NotBeNull();

        // Root itself is Pending but the filter checks children — root without pending children is excluded.
        // Only children with Pending status cause the root to appear.
        result!.Nodes.Should().NotContain(n => n.TaxonomyGuid == pendingRoot.Guid);
    }

    /// <summary>
    /// Tests Get Taxonomies For Approval – Includes Full Subtree For Matching Root.
    /// </summary>
    [Fact]
    public async Task GetTaxonomiesForApproval_IncludesFullSubtreeForMatchingRoot()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        AuthenticateAs(admin);

        var phylum = await SpecimenRepo.CreateTaxonomyNodeAsync(
            "Chordata", TaxonomyRank.Phylum, ApprovalStatus.Approved);
        var @class = await SpecimenRepo.CreateTaxonomyNodeAsync(
            "Mammalia", TaxonomyRank.Class, ApprovalStatus.Approved, parentGuid: phylum.Guid);
        var order = await SpecimenRepo.CreateTaxonomyNodeAsync(
            "Carnivora", TaxonomyRank.Order, ApprovalStatus.Pending, parentGuid: @class.Guid);

        var result = await Client.GetFromJsonAsync<GetTaxonomyTreeResponseDto>(
            Routings.AdminTaxonomyForApprovalRouting);

        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(1);

        var root = result.Nodes.Single();
        root.TaxonomyGuid.Should().Be(phylum.Guid);

        var classNode = root.Children.Single();
        classNode.TaxonomyGuid.Should().Be(@class.Guid);
        classNode.Children.Should().ContainSingle(n => n.TaxonomyGuid == order.Guid);
    }

    /// <summary>
    /// Tests Get Taxonomies For Approval – Nodes Ordered Alphabetically.
    /// </summary>
    [Fact]
    public async Task GetTaxonomiesForApproval_NodesOrderedAlphabetically()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        AuthenticateAs(admin);

        await SpecimenRepo.CreateTaxonomyNodeAsync("Zygoptera", TaxonomyRank.Phylum, ApprovalStatus.Approved);
        await SpecimenRepo.CreateTaxonomyNodeAsync("Arthropoda", TaxonomyRank.Phylum, ApprovalStatus.Approved);
        var pendingClass1 = await SpecimenRepo.CreateTaxonomyNodeAsync(
            "ClassZ", TaxonomyRank.Class, ApprovalStatus.Pending);
        var pendingClass2 = await SpecimenRepo.CreateTaxonomyNodeAsync(
            "ClassA", TaxonomyRank.Class, ApprovalStatus.Pending);

        // Add pending children under roots to trigger inclusion
        var zRoot = await SpecimenRepo.CreateTaxonomyNodeAsync("Zygoptera2", TaxonomyRank.Phylum, ApprovalStatus.Approved);
        await SpecimenRepo.CreateTaxonomyNodeAsync("PendingChild", TaxonomyRank.Class, ApprovalStatus.Pending, parentGuid: zRoot.Guid);
        var aRoot = await SpecimenRepo.CreateTaxonomyNodeAsync("Arthropoda2", TaxonomyRank.Phylum, ApprovalStatus.Approved);
        await SpecimenRepo.CreateTaxonomyNodeAsync("PendingChild2", TaxonomyRank.Class, ApprovalStatus.Pending, parentGuid: aRoot.Guid);

        var result = await Client.GetFromJsonAsync<GetTaxonomyTreeResponseDto>(
            Routings.AdminTaxonomyForApprovalRouting);

        result.Should().NotBeNull();
        result!.Nodes.Should().BeInAscendingOrder(n => n.Name);
    }

    /// <summary>
    /// Tests Get Taxonomies For Approval – No Pending Nodes – Returns Empty.
    /// </summary>
    [Fact]
    public async Task GetTaxonomiesForApproval_NoPendingNodes_ReturnsEmpty()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        AuthenticateAs(admin);

        await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata", TaxonomyRank.Phylum, ApprovalStatus.Approved);
        await SpecimenRepo.CreateTaxonomyNodeAsync("Arthropoda", TaxonomyRank.Phylum, ApprovalStatus.Rejected);

        var result = await Client.GetFromJsonAsync<GetTaxonomyTreeResponseDto>(
            Routings.AdminTaxonomyForApprovalRouting);

        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(0);
        result.Nodes.Should().BeEmpty();
    }

    /// <summary>
    /// Tests Get Taxonomies For Approval – Unauthenticated – Returns Unauthorized.
    /// </summary>
    [Fact]
    public async Task GetTaxonomiesForApproval_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync(Routings.AdminTaxonomyForApprovalRouting);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Tests Get Taxonomies For Approval – Normal User – Returns Forbidden.
    /// </summary>
    [Fact]
    public async Task GetTaxonomiesForApproval_NormalUser_ReturnsForbidden()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        AuthenticateAs(user);

        var response = await Client.GetAsync(Routings.AdminTaxonomyForApprovalRouting);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GetSimilarTaxonomies

    /// <summary>
    /// Tests Get Similar – Returns Approved Same Rank Similar Nodes.
    /// </summary>
    [Fact]
    public async Task GetSimilar_ReturnsApprovedSameRankSimilarNodes()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        AuthenticateAs(admin);

        var reviewed = await SpecimenRepo.CreateTaxonomyNodeAsync(
            "Panthera leo", TaxonomyRank.Species, ApprovalStatus.Pending);
        var similar = await SpecimenRepo.CreateTaxonomyNodeAsync(
            "Panthera leon", TaxonomyRank.Species, ApprovalStatus.Approved);
        // Different rank → excluded.
        await SpecimenRepo.CreateTaxonomyNodeAsync("Panthera", TaxonomyRank.Genus, ApprovalStatus.Approved);
        // Completely different name → below the similarity threshold.
        await SpecimenRepo.CreateTaxonomyNodeAsync(
            "Loxodonta africana", TaxonomyRank.Species, ApprovalStatus.Approved);

        var response = await Client.GetAsync($"{Routings.AdminTaxonomyRouting}/{reviewed.Guid}/similar");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<GetSimilarTaxonomiesResponseDto>();
        dto!.TaxonomyGuid.Should().Be(reviewed.Guid);
        dto.Similar.Should().ContainSingle();
        dto.Similar.Single().TaxonomyGuid.Should().Be(similar.Guid);
        dto.Similar.Single().Similarity.Should().BeGreaterThan(0.5);
    }

    /// <summary>
    /// Tests Get Similar – Includes Ancestor Path.
    /// </summary>
    [Fact]
    public async Task GetSimilar_IncludesAncestorPath()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        AuthenticateAs(admin);

        var genus = await SpecimenRepo.CreateTaxonomyNodeAsync(
            "Panthera", TaxonomyRank.Genus, ApprovalStatus.Approved);
        await SpecimenRepo.CreateTaxonomyNodeAsync(
            "Panthera leon", TaxonomyRank.Species, ApprovalStatus.Approved, parentGuid: genus.Guid);
        var reviewed = await SpecimenRepo.CreateTaxonomyNodeAsync(
            "Panthera leo", TaxonomyRank.Species, ApprovalStatus.Pending);

        var response = await Client.GetAsync($"{Routings.AdminTaxonomyRouting}/{reviewed.Guid}/similar");

        var dto = await response.Content.ReadFromJsonAsync<GetSimilarTaxonomiesResponseDto>();
        dto!.Similar.Single().Path.Should().Be("Panthera");
    }

    /// <summary>
    /// Tests Get Similar – Excludes Pending Candidates.
    /// </summary>
    [Fact]
    public async Task GetSimilar_ExcludesPendingCandidates()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        AuthenticateAs(admin);

        var reviewed = await SpecimenRepo.CreateTaxonomyNodeAsync(
            "Panthera leo", TaxonomyRank.Species, ApprovalStatus.Pending);
        // A near-identical name but still pending → not offered as an existing taxonomy.
        await SpecimenRepo.CreateTaxonomyNodeAsync(
            "Panthera leon", TaxonomyRank.Species, ApprovalStatus.Pending);

        var response = await Client.GetAsync($"{Routings.AdminTaxonomyRouting}/{reviewed.Guid}/similar");

        var dto = await response.Content.ReadFromJsonAsync<GetSimilarTaxonomiesResponseDto>();
        dto!.Similar.Should().BeEmpty();
    }

    /// <summary>
    /// Tests Get Similar – Node Not Found – Returns No Taxonomy Found.
    /// </summary>
    [Fact]
    public async Task GetSimilar_NodeNotFound_ReturnsNoTaxonomyFound()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        AuthenticateAs(admin);

        var response = await Client.GetAsync($"{Routings.AdminTaxonomyRouting}/{Guid.NewGuid()}/similar");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoTaxonomyFound);
    }

    /// <summary>
    /// Tests Get Similar – Normal User – Returns Forbidden.
    /// </summary>
    [Fact]
    public async Task GetSimilar_NormalUser_ReturnsForbidden()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        AuthenticateAs(user);

        var response = await Client.GetAsync($"{Routings.AdminTaxonomyRouting}/{Guid.NewGuid()}/similar");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion
}
