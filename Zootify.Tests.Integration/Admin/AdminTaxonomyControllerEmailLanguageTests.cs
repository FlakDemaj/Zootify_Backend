using Zootify_Backend.DTOs.Admin.Request;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Admin;

/// <summary>
/// Verifies that taxonomy approval and rejection notification emails are sent in the affected user's
/// stored language preference (<c>UserSettings.Language</c>). The service passes that language
/// explicitly to <c>IBrevoProvider</c>, so the test verifies the language argument the provider
/// receives — independent of the moderating admin's <c>Accept-Language</c> header.
///
/// Both emails are only sent when there is at least one affected user (here: the owner of an affected
/// specimen), so each scenario sets up a specimen owned by a user with a chosen settings language.
/// </summary>
public class AdminTaxonomyControllerEmailLanguageTests : IntegrationTestBase
{
    public AdminTaxonomyControllerEmailLanguageTests(IntegrationTestFixture fixture) : base(fixture) { }

    /// <summary>
    /// Tests Approve Taxonomy – Passes Affected User Language To Email.
    /// </summary>
    [Theory]
    [InlineData("en")]
    [InlineData("de")]
    public async Task ApproveTaxonomy_PassesAffectedUserLanguageToEmail(string userLanguage)
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var user = await UserRepo.CreateUserAsync("user@test.de");
        await CreateUserSettingsInDbAsync(user.Guid, language: userLanguage);
        AuthenticateAs(admin);

        // Approved parent + pending species so that approving the species completes the chain and
        // activates the user's pending specimen, making that user an email recipient.
        var phylum = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata", TaxonomyRank.Phylum, ApprovalStatus.Approved);
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync(
            "leo", TaxonomyRank.Species, ApprovalStatus.Pending, parentGuid: phylum.Guid);

        var geo = await SpecimenRepo.CreateGeographicLocationAsync();
        await SpecimenRepo.CreateSpecimenAsync(
            species.Guid, geo.Guid, status: SpecimenStatus.Pending, createdByUserGuid: user.Guid);

        Fixture.BrevoMock
            .Setup(b => b.SendTaxonomyApprovedEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<TaxonomyRank>(), It.IsAny<string?>()))
            .ReturnsAsync(true);

        var response = await Client.PostAsync(
            $"{Routings.AdminTaxonomyRouting}/{species.Guid}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Fixture.BrevoMock.Verify(b => b.SendTaxonomyApprovedEmail(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<TaxonomyRank>(), userLanguage), Times.Once);
    }

    /// <summary>
    /// Tests Reject Taxonomy – Passes Affected User Language To Email.
    /// </summary>
    [Theory]
    [InlineData("en")]
    [InlineData("de")]
    public async Task RejectTaxonomy_PassesAffectedUserLanguageToEmail(string userLanguage)
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var user = await UserRepo.CreateUserAsync("user@test.de");
        await CreateUserSettingsInDbAsync(user.Guid, language: userLanguage);
        AuthenticateAs(admin);

        // A specimen owned by the user under the rejected subtree makes that user an email recipient.
        var phylum = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata", TaxonomyRank.Phylum, ApprovalStatus.Pending);
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync(
            "leo", TaxonomyRank.Species, ApprovalStatus.Pending, parentGuid: phylum.Guid);

        var geo = await SpecimenRepo.CreateGeographicLocationAsync();
        await SpecimenRepo.CreateSpecimenAsync(
            species.Guid, geo.Guid, status: SpecimenStatus.Active, createdByUserGuid: user.Guid);

        Fixture.BrevoMock
            .Setup(b => b.SendTaxonomyRejectedEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<TaxonomyRank>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(true);

        var request = new RejectTaxonomyRequestDto { Comment = "Incorrect classification." };

        var response = await Client.PostAsJsonAsync(
            $"{Routings.AdminTaxonomyRouting}/{phylum.Guid}/reject", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Fixture.BrevoMock.Verify(b => b.SendTaxonomyRejectedEmail(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<TaxonomyRank>(), It.IsAny<string>(), userLanguage), Times.Once);
    }
}
