using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Common;

/// <summary>
/// Tests for Common Loan Request Controller — the self-service request/approval workflow.
/// </summary>
public class CommonLoanRequestControllerTests : IntegrationTestBase
{
    public CommonLoanRequestControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    private void SetupBrevoReturnsTrue()
    {
        Fixture.BrevoMock
            .Setup(b => b.SendLoanRequestSubmittedEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string?>()))
            .ReturnsAsync(true);
        Fixture.BrevoMock
            .Setup(b => b.SendLoanRequestRejectedEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(true);
        Fixture.BrevoMock
            .Setup(b => b.SendLoanConfirmationEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(true);
    }

    private async Task<SpecimenEntity> CreateSpecimenAsync(Guid? createdByUserGuid = null, string name = "Lion")
    {
        var geo     = await SpecimenRepo.CreateGeographicLocationAsync();
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        return await SpecimenRepo.CreateSpecimenAsync(species.Guid, geo.Guid, createdByUserGuid: createdByUserGuid, name: name);
    }

    private static CreateLoanRequestRequestDto RequestFor(Guid specimenGuid) => new()
    {
        SpecimenGuid   = specimenGuid,
        DesiredDueDate = DateTime.UtcNow.AddDays(21),
        Note           = "Please.",
    };

    #region CreateRequest

    /// <summary>
    /// Tests Create Request – Success – Persists Pending Request.
    /// </summary>
    [Fact]
    public async Task CreateRequest_Success_PersistsPending()
    {
        await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var creator   = await UserRepo.CreateUserAsync("creator@test.de");
        var requester = await UserRepo.CreateUserAsync("requester@test.de");
        var specimen  = await CreateSpecimenAsync(createdByUserGuid: creator.Guid);
        AuthenticateAs(requester);

        var response = await Client.PostAsJsonAsync(Routings.CommonLoanRequestRouting, RequestFor(specimen.Guid));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CreatedObjectResponseDto>();
        result!.CreatedObjectGuid.Should().NotBeEmpty();

        var stored = await LoanRequestRepo.GetByGuidAsync(result.CreatedObjectGuid);
        stored!.Status.Should().Be(LoanRequestStatus.Pending);
        stored.RequesterGuid.Should().Be(requester.Guid);
        stored.SpecimenGuid.Should().Be(specimen.Guid);
    }

    /// <summary>
    /// Tests Create Request – Specimen Already Requested – Returns Error.
    /// </summary>
    [Fact]
    public async Task CreateRequest_SpecimenAlreadyRequested_ReturnsError()
    {
        var creator   = await UserRepo.CreateUserAsync("creator@test.de");
        var requester = await UserRepo.CreateUserAsync("requester@test.de");
        var specimen  = await CreateSpecimenAsync(createdByUserGuid: creator.Guid);
        await LoanRequestRepo.CreateLoanRequestAsync(specimen.Guid, requester.Guid);
        AuthenticateAs(requester);

        var response = await Client.PostAsJsonAsync(Routings.CommonLoanRequestRouting, RequestFor(specimen.Guid));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.SpecimenAlreadyRequested);
    }

    /// <summary>
    /// Tests Create Request – Specimen Already On Loan – Returns Error.
    /// </summary>
    [Fact]
    public async Task CreateRequest_SpecimenAlreadyOnLoan_ReturnsError()
    {
        var admin     = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var requester = await UserRepo.CreateUserAsync("requester@test.de");
        var specimen  = await CreateSpecimenAsync();
        await LoanRepo.CreateLoanAsync(specimen.Guid, admin.Guid, requester.Guid, DateTime.UtcNow.AddDays(14));
        AuthenticateAs(requester);

        var response = await Client.PostAsJsonAsync(Routings.CommonLoanRequestRouting, RequestFor(specimen.Guid));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.SpecimenAlreadyOnLoan);
    }

    /// <summary>
    /// Tests Create Request – Specimen Not Found – Returns Error.
    /// </summary>
    [Fact]
    public async Task CreateRequest_SpecimenNotFound_ReturnsError()
    {
        var requester = await UserRepo.CreateUserAsync("requester@test.de");
        AuthenticateAs(requester);

        var response = await Client.PostAsJsonAsync(Routings.CommonLoanRequestRouting, RequestFor(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoSpecimenFound);
    }

    /// <summary>
    /// Tests Create Request – Unauthenticated – Returns Unauthorized.
    /// </summary>
    [Fact]
    public async Task CreateRequest_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync(Routings.CommonLoanRequestRouting, RequestFor(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Tests Create Request – Notifies Admins And Creator Via Push.
    /// </summary>
    [Fact]
    public async Task CreateRequest_NotifiesAdminsAndCreator()
    {
        SetupBrevoReturnsTrue();
        await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var creator   = await UserRepo.CreateUserAsync("creator@test.de");
        var requester = await UserRepo.CreateUserAsync("requester@test.de");
        var specimen  = await CreateSpecimenAsync(createdByUserGuid: creator.Guid);
        AuthenticateAs(requester);

        var response = await Client.PostAsJsonAsync(Routings.CommonLoanRequestRouting, RequestFor(specimen.Guid));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // One admin + the creator are notified (the requester is excluded).
        VerifyPushNotificationSent("Ausleih-Anfrage", Times.Exactly(2));
    }

    #endregion

    #region Approve

    /// <summary>
    /// Tests Approve – Admin And Creator – Creates Loan After Both Approvals.
    /// </summary>
    [Fact]
    public async Task Approve_AdminAndCreator_CreatesLoanAfterBoth()
    {
        SetupBrevoReturnsTrue();
        var admin     = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var creator   = await UserRepo.CreateUserAsync("creator@test.de");
        var requester = await UserRepo.CreateUserAsync("requester@test.de");
        var specimen  = await CreateSpecimenAsync(createdByUserGuid: creator.Guid);
        var request   = await LoanRequestRepo.CreateLoanRequestAsync(
            specimen.Guid, requester.Guid, desiredDueDate: DateTime.UtcNow.AddDays(21));

        AuthenticateAs(admin);
        var afterAdmin = await Client.PostAsync($"{Routings.CommonLoanRequestRouting}/{request.Guid}/approve", null);
        afterAdmin.StatusCode.Should().Be(HttpStatusCode.OK);
        (await LoanRequestRepo.GetByGuidAsync(request.Guid))!.Status.Should().Be(LoanRequestStatus.Pending);

        AuthenticateAs(creator);
        var afterCreator = await Client.PostAsync($"{Routings.CommonLoanRequestRouting}/{request.Guid}/approve", null);
        afterCreator.StatusCode.Should().Be(HttpStatusCode.OK);

        var stored = await LoanRequestRepo.GetByGuidAsync(request.Guid);
        stored!.Status.Should().Be(LoanRequestStatus.Approved);
        stored.CreatedLoanGuid.Should().NotBeNull();

        var loan = await GetLoanFromDbAsync(stored.CreatedLoanGuid!.Value);
        loan!.BorrowerGuid.Should().Be(requester.Guid);
        loan.LenderGuid.Should().Be(admin.Guid);
    }

    /// <summary>
    /// Tests Approve – Creator Is Admin – Single Approval Creates Loan.
    /// </summary>
    [Fact]
    public async Task Approve_CreatorIsAdmin_SingleApprovalCreatesLoan()
    {
        SetupBrevoReturnsTrue();
        var creatorAdmin = await UserRepo.CreateUserAsync("creatoradmin@test.de", role: UserRole.Admin);
        var requester    = await UserRepo.CreateUserAsync("requester@test.de");
        var specimen     = await CreateSpecimenAsync(createdByUserGuid: creatorAdmin.Guid);
        var request      = await LoanRequestRepo.CreateLoanRequestAsync(specimen.Guid, requester.Guid);
        AuthenticateAs(creatorAdmin);

        var response = await Client.PostAsync($"{Routings.CommonLoanRequestRouting}/{request.Guid}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await LoanRequestRepo.GetByGuidAsync(request.Guid);
        stored!.Status.Should().Be(LoanRequestStatus.Approved);
        stored.CreatedLoanGuid.Should().NotBeNull();
    }

    /// <summary>
    /// Tests Approve – No Creator – Single Admin Approval Creates Loan.
    /// </summary>
    [Fact]
    public async Task Approve_NoCreator_SingleAdminApprovalCreatesLoan()
    {
        SetupBrevoReturnsTrue();
        var admin     = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var requester = await UserRepo.CreateUserAsync("requester@test.de");
        var specimen  = await CreateSpecimenAsync();
        var request   = await LoanRequestRepo.CreateLoanRequestAsync(specimen.Guid, requester.Guid);
        AuthenticateAs(admin);

        var response = await Client.PostAsync($"{Routings.CommonLoanRequestRouting}/{request.Guid}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await LoanRequestRepo.GetByGuidAsync(request.Guid))!.Status.Should().Be(LoanRequestStatus.Approved);
    }

    /// <summary>
    /// Tests Approve – Requester Owns Specimen – Only Admin Approval Needed.
    /// </summary>
    [Fact]
    public async Task Approve_RequesterOwnsSpecimen_OnlyAdminNeeded()
    {
        SetupBrevoReturnsTrue();
        var admin     = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var requester = await UserRepo.CreateUserAsync("requester@test.de");
        var specimen  = await CreateSpecimenAsync(createdByUserGuid: requester.Guid);
        var request   = await LoanRequestRepo.CreateLoanRequestAsync(specimen.Guid, requester.Guid);
        AuthenticateAs(admin);

        var response = await Client.PostAsync($"{Routings.CommonLoanRequestRouting}/{request.Guid}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await LoanRequestRepo.GetByGuidAsync(request.Guid))!.Status.Should().Be(LoanRequestStatus.Approved);
    }

    /// <summary>
    /// Tests Approve – Requester Cannot Approve Own Request.
    /// </summary>
    [Fact]
    public async Task Approve_Requester_NotAllowed()
    {
        await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var requester = await UserRepo.CreateUserAsync("requester@test.de");
        var specimen  = await CreateSpecimenAsync(createdByUserGuid: requester.Guid);
        var request   = await LoanRequestRepo.CreateLoanRequestAsync(specimen.Guid, requester.Guid);
        AuthenticateAs(requester);

        var response = await Client.PostAsync($"{Routings.CommonLoanRequestRouting}/{request.Guid}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NotAllowedToDecideLoanRequest);
    }

    /// <summary>
    /// Tests Approve – Unrelated Normal User – Not Allowed.
    /// </summary>
    [Fact]
    public async Task Approve_UnrelatedUser_NotAllowed()
    {
        var creator   = await UserRepo.CreateUserAsync("creator@test.de");
        var requester = await UserRepo.CreateUserAsync("requester@test.de");
        var outsider  = await UserRepo.CreateUserAsync("outsider@test.de");
        var specimen  = await CreateSpecimenAsync(createdByUserGuid: creator.Guid);
        var request   = await LoanRequestRepo.CreateLoanRequestAsync(specimen.Guid, requester.Guid);
        AuthenticateAs(outsider);

        var response = await Client.PostAsync($"{Routings.CommonLoanRequestRouting}/{request.Guid}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NotAllowedToDecideLoanRequest);
    }

    /// <summary>
    /// Tests Approve – Second Admin While Creator Pending – Already Decided.
    /// </summary>
    [Fact]
    public async Task Approve_SecondAdminWhileCreatorPending_AlreadyDecided()
    {
        var firstAdmin  = await UserRepo.CreateUserAsync("admin1@test.de", role: UserRole.Admin);
        var secondAdmin = await UserRepo.CreateUserAsync("admin2@test.de", role: UserRole.Admin);
        var creator     = await UserRepo.CreateUserAsync("creator@test.de");
        var requester   = await UserRepo.CreateUserAsync("requester@test.de");
        var specimen    = await CreateSpecimenAsync(createdByUserGuid: creator.Guid);
        var request     = await LoanRequestRepo.CreateLoanRequestAsync(
            specimen.Guid, requester.Guid, adminApproverGuid: firstAdmin.Guid, adminApprovedAt: DateTime.UtcNow);
        AuthenticateAs(secondAdmin);

        var response = await Client.PostAsync($"{Routings.CommonLoanRequestRouting}/{request.Guid}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.LoanRequestAlreadyDecided);
    }

    /// <summary>
    /// Tests Approve – Already Decided Request – Returns Error.
    /// </summary>
    [Fact]
    public async Task Approve_AlreadyDecided_ReturnsError()
    {
        var admin     = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var requester = await UserRepo.CreateUserAsync("requester@test.de");
        var specimen  = await CreateSpecimenAsync();
        var request   = await LoanRequestRepo.CreateLoanRequestAsync(
            specimen.Guid, requester.Guid, status: LoanRequestStatus.Rejected);
        AuthenticateAs(admin);

        var response = await Client.PostAsync($"{Routings.CommonLoanRequestRouting}/{request.Guid}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.LoanRequestAlreadyDecided);
    }

    /// <summary>
    /// Tests Approve – Not Found – Returns Error.
    /// </summary>
    [Fact]
    public async Task Approve_NotFound_ReturnsError()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        AuthenticateAs(admin);

        var response = await Client.PostAsync($"{Routings.CommonLoanRequestRouting}/{Guid.NewGuid()}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.LoanRequestNotFound);
    }

    /// <summary>
    /// Tests Approve – Notifies Requester On Full Approval.
    /// </summary>
    [Fact]
    public async Task Approve_NotifiesRequesterOnFullApproval()
    {
        SetupBrevoReturnsTrue();
        var admin     = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var requester = await UserRepo.CreateUserAsync("requester@test.de");
        var specimen  = await CreateSpecimenAsync();
        var request   = await LoanRequestRepo.CreateLoanRequestAsync(specimen.Guid, requester.Guid);
        AuthenticateAs(admin);

        var response = await Client.PostAsync($"{Routings.CommonLoanRequestRouting}/{request.Guid}/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        VerifyPushNotificationSent("genehmigt", Times.Once());
    }

    #endregion

    #region Reject

    /// <summary>
    /// Tests Reject – By Admin – Persists Reason And Notifies Requester.
    /// </summary>
    [Fact]
    public async Task Reject_ByAdmin_PersistsReasonAndNotifies()
    {
        SetupBrevoReturnsTrue();
        var admin     = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var requester = await UserRepo.CreateUserAsync("requester@test.de");
        var specimen  = await CreateSpecimenAsync();
        var request   = await LoanRequestRepo.CreateLoanRequestAsync(specimen.Guid, requester.Guid);
        AuthenticateAs(admin);

        var response = await Client.PostAsJsonAsync(
            $"{Routings.CommonLoanRequestRouting}/{request.Guid}/reject",
            new RejectLoanRequestRequestDto { Reason = "Currently on display." });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await LoanRequestRepo.GetByGuidAsync(request.Guid);
        stored!.Status.Should().Be(LoanRequestStatus.Rejected);
        stored.RejectionReason.Should().Be("Currently on display.");
        VerifyPushNotificationSent("abgelehnt", Times.Once());
    }

    /// <summary>
    /// Tests Reject – By Creator – Success.
    /// </summary>
    [Fact]
    public async Task Reject_ByCreator_Success()
    {
        SetupBrevoReturnsTrue();
        await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var creator   = await UserRepo.CreateUserAsync("creator@test.de");
        var requester = await UserRepo.CreateUserAsync("requester@test.de");
        var specimen  = await CreateSpecimenAsync(createdByUserGuid: creator.Guid);
        var request   = await LoanRequestRepo.CreateLoanRequestAsync(specimen.Guid, requester.Guid);
        AuthenticateAs(creator);

        var response = await Client.PostAsJsonAsync(
            $"{Routings.CommonLoanRequestRouting}/{request.Guid}/reject",
            new RejectLoanRequestRequestDto { Reason = "No." });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await LoanRequestRepo.GetByGuidAsync(request.Guid))!.Status.Should().Be(LoanRequestStatus.Rejected);
    }

    /// <summary>
    /// Tests Reject – Unrelated User – Not Allowed.
    /// </summary>
    [Fact]
    public async Task Reject_UnrelatedUser_NotAllowed()
    {
        var creator   = await UserRepo.CreateUserAsync("creator@test.de");
        var requester = await UserRepo.CreateUserAsync("requester@test.de");
        var outsider  = await UserRepo.CreateUserAsync("outsider@test.de");
        var specimen  = await CreateSpecimenAsync(createdByUserGuid: creator.Guid);
        var request   = await LoanRequestRepo.CreateLoanRequestAsync(specimen.Guid, requester.Guid);
        AuthenticateAs(outsider);

        var response = await Client.PostAsJsonAsync(
            $"{Routings.CommonLoanRequestRouting}/{request.Guid}/reject",
            new RejectLoanRequestRequestDto { Reason = "x" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NotAllowedToDecideLoanRequest);
    }

    #endregion

    #region Pending

    /// <summary>
    /// Tests Get Pending – Admin Sees Requests Awaiting Admin Approval.
    /// </summary>
    [Fact]
    public async Task GetPending_Admin_SeesAwaitingAdmin()
    {
        var admin     = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var creator   = await UserRepo.CreateUserAsync("creator@test.de");
        var requester = await UserRepo.CreateUserAsync("requester@test.de");
        var specimen  = await CreateSpecimenAsync(createdByUserGuid: creator.Guid);
        var request   = await LoanRequestRepo.CreateLoanRequestAsync(specimen.Guid, requester.Guid);
        AuthenticateAs(admin);

        var result = await Client.GetFromJsonAsync<List<GetLoanRequestOverviewDto>>(Routings.CommonLoanRequestPendingRouting);

        result.Should().NotBeNull();
        result!.Should().ContainSingle(r => r.RequestGuid == request.Guid);
    }

    /// <summary>
    /// Tests Get Pending – Creator Sees Requests For Own Specimens.
    /// </summary>
    [Fact]
    public async Task GetPending_Creator_SeesOwnSpecimenRequests()
    {
        await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var creator   = await UserRepo.CreateUserAsync("creator@test.de");
        var requester = await UserRepo.CreateUserAsync("requester@test.de");
        var specimen  = await CreateSpecimenAsync(createdByUserGuid: creator.Guid);
        var request   = await LoanRequestRepo.CreateLoanRequestAsync(specimen.Guid, requester.Guid);
        AuthenticateAs(creator);

        var result = await Client.GetFromJsonAsync<List<GetLoanRequestOverviewDto>>(Routings.CommonLoanRequestPendingRouting);

        result.Should().NotBeNull();
        result!.Should().ContainSingle(r => r.RequestGuid == request.Guid);
    }

    /// <summary>
    /// Tests Get Pending – Unrelated Normal User – Sees Empty.
    /// </summary>
    [Fact]
    public async Task GetPending_UnrelatedUser_SeesEmpty()
    {
        var creator   = await UserRepo.CreateUserAsync("creator@test.de");
        var requester = await UserRepo.CreateUserAsync("requester@test.de");
        var outsider  = await UserRepo.CreateUserAsync("outsider@test.de");
        var specimen  = await CreateSpecimenAsync(createdByUserGuid: creator.Guid);
        await LoanRequestRepo.CreateLoanRequestAsync(specimen.Guid, requester.Guid);
        AuthenticateAs(outsider);

        var result = await Client.GetFromJsonAsync<List<GetLoanRequestOverviewDto>>(Routings.CommonLoanRequestPendingRouting);

        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    /// <summary>
    /// Tests Get Pending – Excludes The Caller's Own Requests.
    /// </summary>
    [Fact]
    public async Task GetPending_ExcludesOwnRequests()
    {
        // An admin who is also the requester should not see their own request to decide on.
        var admin    = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var specimen = await CreateSpecimenAsync();
        await LoanRequestRepo.CreateLoanRequestAsync(specimen.Guid, admin.Guid);
        AuthenticateAs(admin);

        var result = await Client.GetFromJsonAsync<List<GetLoanRequestOverviewDto>>(Routings.CommonLoanRequestPendingRouting);

        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    #endregion

    #region GetDetail

    /// <summary>
    /// Tests Get Detail – Requester Can View Own Request.
    /// </summary>
    [Fact]
    public async Task GetDetail_Requester_Success()
    {
        var requester = await UserRepo.CreateUserAsync("requester@test.de");
        var specimen  = await CreateSpecimenAsync();
        var request   = await LoanRequestRepo.CreateLoanRequestAsync(specimen.Guid, requester.Guid);
        AuthenticateAs(requester);

        var result = await Client.GetFromJsonAsync<GetLoanRequestDetailDto>(
            $"{Routings.CommonLoanRequestRouting}/{request.Guid}");

        result.Should().NotBeNull();
        result!.RequestGuid.Should().Be(request.Guid);
        result.RequesterGuid.Should().Be(requester.Guid);
    }

    /// <summary>
    /// Tests Get Detail – Outsider – Returns Not Found.
    /// </summary>
    [Fact]
    public async Task GetDetail_Outsider_ReturnsNotFound()
    {
        var requester = await UserRepo.CreateUserAsync("requester@test.de");
        var outsider  = await UserRepo.CreateUserAsync("outsider@test.de");
        var specimen  = await CreateSpecimenAsync();
        var request   = await LoanRequestRepo.CreateLoanRequestAsync(specimen.Guid, requester.Guid);
        AuthenticateAs(outsider);

        var response = await Client.GetAsync($"{Routings.CommonLoanRequestRouting}/{request.Guid}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.LoanRequestNotFound);
    }

    #endregion
}
