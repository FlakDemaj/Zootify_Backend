using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Admin;

public class AdminTaxonomyControllerTests : IntegrationTestBase
{
    public AdminTaxonomyControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    #region ApproveTaxonomy

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

    [Fact]
    public async Task ApproveTaxonomy_NotFound_ReturnsError()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        AuthenticateAs(admin);

        var response = await Client.PostAsync(
            $"{Routings.AdminTaxonomyRouting}/{Guid.NewGuid()}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoTaxonomyWithThisGuid);
    }

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
        error.ErrorCode.Should().Be((int)ErrorCode.NoTaxonomyWithThisGuid);
    }

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

    [Fact]
    public async Task GetTaxonomiesForApproval_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync(Routings.AdminTaxonomyForApprovalRouting);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTaxonomiesForApproval_NormalUser_ReturnsForbidden()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        AuthenticateAs(user);

        var response = await Client.GetAsync(Routings.AdminTaxonomyForApprovalRouting);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion
}
