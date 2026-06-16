using Zootify_Backend.DTOs.Admin.Request;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Admin;

/// <summary>
/// Verifies that taxonomy approval/rejection emails are accompanied by an in-app push notification to
/// each affected user, that both honor the user's notification settings, and that the push message is
/// localized to the user's language.
/// </summary>
public class AdminTaxonomyControllerNotificationTests : IntegrationTestBase
{
    public AdminTaxonomyControllerNotificationTests(IntegrationTestFixture fixture) : base(fixture) { }

    private void SetupBrevoReturnsTrue()
    {
        Fixture.BrevoMock
            .Setup(b => b.SendTaxonomyApprovedEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<TaxonomyRank>(), It.IsAny<string?>()))
            .ReturnsAsync(true);
        Fixture.BrevoMock
            .Setup(b => b.SendTaxonomyRejectedEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<TaxonomyRank>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(true);
    }

    /// <summary>
    /// Creates an admin, an affected user (with the given notification settings), and an approved
    /// phylum + pending species with a pending specimen owned by the user. Approving the species makes
    /// the user an affected recipient.
    /// </summary>
    private async Task<(UserEntity user, TaxonomyNodeEntity species)> ArrangeApproveAsync(
        bool sendEmail = true, bool sendPush = true, string language = "de")
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var user = await UserRepo.CreateUserAsync("user@test.de");
        await CreateUserSettingsInDbAsync(user.Guid, sendEmail: sendEmail, sendPush: sendPush, language: language);
        AuthenticateAs(admin);

        var phylum = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata", TaxonomyRank.Phylum, ApprovalStatus.Approved);
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync(
            "leo", TaxonomyRank.Species, ApprovalStatus.Pending, parentGuid: phylum.Guid);
        var geo = await SpecimenRepo.CreateGeographicLocationAsync();
        await SpecimenRepo.CreateSpecimenAsync(
            species.Guid, geo.Guid, status: SpecimenStatus.Pending, createdByUserGuid: user.Guid);
        return (user, species);
    }

    /// <summary>
    /// Tests Approve Taxonomy – Sends Push To Affected User In User Language.
    /// </summary>
    [Theory]
    [InlineData("en", "approved")]
    [InlineData("de", "genehmigt")]
    public async Task ApproveTaxonomy_SendsPushToAffectedUserInUserLanguage(string language, string keyword)
    {
        SetupBrevoReturnsTrue();
        var (_, species) = await ArrangeApproveAsync(language: language);

        var response = await Client.PostAsync(
            $"{Routings.AdminTaxonomyRouting}/{species.Guid}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        VerifyPushNotificationSent(keyword, Times.Once());
    }

    /// <summary>
    /// Tests Approve Taxonomy – Push Disabled – Sends Email But No Push.
    /// </summary>
    [Fact]
    public async Task ApproveTaxonomy_PushDisabled_SendsEmailButNoPush()
    {
        SetupBrevoReturnsTrue();
        var (_, species) = await ArrangeApproveAsync(sendPush: false);

        var response = await Client.PostAsync(
            $"{Routings.AdminTaxonomyRouting}/{species.Guid}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        VerifyNoPushNotificationSent();
        Fixture.BrevoMock.Verify(b => b.SendTaxonomyApprovedEmail(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<TaxonomyRank>(), It.IsAny<string?>()), Times.Once);
    }

    /// <summary>
    /// Tests Reject Taxonomy – Sends Push To Affected User In User Language.
    /// </summary>
    [Theory]
    [InlineData("en", "rejected")]
    [InlineData("de", "abgelehnt")]
    public async Task RejectTaxonomy_SendsPushToAffectedUserInUserLanguage(string language, string keyword)
    {
        SetupBrevoReturnsTrue();

        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var user = await UserRepo.CreateUserAsync("user@test.de");
        await CreateUserSettingsInDbAsync(user.Guid, language: language);
        AuthenticateAs(admin);

        var phylum = await SpecimenRepo.CreateTaxonomyNodeAsync("Chordata", TaxonomyRank.Phylum, ApprovalStatus.Pending);
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync(
            "leo", TaxonomyRank.Species, ApprovalStatus.Approved, parentGuid: phylum.Guid);
        var geo = await SpecimenRepo.CreateGeographicLocationAsync();
        await SpecimenRepo.CreateSpecimenAsync(
            species.Guid, geo.Guid, status: SpecimenStatus.Active, createdByUserGuid: user.Guid);

        var response = await Client.PostAsJsonAsync(
            $"{Routings.AdminTaxonomyRouting}/{phylum.Guid}/reject",
            new RejectTaxonomyRequestDto { Comment = "Incorrect classification." });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        VerifyPushNotificationSent(keyword, Times.Once());
    }
}
