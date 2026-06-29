namespace Zootify.Tests.Unit.Domain;

/// <summary>
/// Tests for Loan Request Entity — the dual-gate approval logic.
/// </summary>
public class LoanRequestEntityTests
{
    private static readonly Guid Requester = Guid.NewGuid();
    private static readonly Guid Admin     = Guid.NewGuid();
    private static readonly Guid Creator   = Guid.NewGuid();

    private static LoanRequestEntity CreateRequest()
        => LoanRequestEntity.CreateRequest(Guid.NewGuid(), Requester, DateTime.UtcNow.AddDays(14), note: null);

    #region CreateRequest

    /// <summary>
    /// Tests Create Request – Initializes Pending With Request Date.
    /// </summary>
    [Fact]
    public void CreateRequest_InitializesPending()
    {
        var before = DateTime.UtcNow;
        var request = CreateRequest();

        request.Status.Should().Be(LoanRequestStatus.Pending);
        request.RequesterGuid.Should().Be(Requester);
        request.RequestedAt.Should().BeOnOrAfter(before);
        request.AdminApprovedAt.Should().BeNull();
        request.CreatorApprovedAt.Should().BeNull();
    }

    #endregion

    #region RequiresCreatorApproval

    /// <summary>
    /// Tests Requires Creator Approval – No Creator – False.
    /// </summary>
    [Fact]
    public void RequiresCreatorApproval_NoCreator_False()
    {
        CreateRequest().RequiresCreatorApproval(null).Should().BeFalse();
    }

    /// <summary>
    /// Tests Requires Creator Approval – Creator Is Requester – False.
    /// </summary>
    [Fact]
    public void RequiresCreatorApproval_CreatorIsRequester_False()
    {
        CreateRequest().RequiresCreatorApproval(Requester).Should().BeFalse();
    }

    /// <summary>
    /// Tests Requires Creator Approval – Distinct Creator – True.
    /// </summary>
    [Fact]
    public void RequiresCreatorApproval_DistinctCreator_True()
    {
        CreateRequest().RequiresCreatorApproval(Creator).Should().BeTrue();
    }

    #endregion

    #region RegisterApproval

    /// <summary>
    /// Tests Register Approval – Admin Only Needed – Approves Immediately.
    /// </summary>
    [Fact]
    public void RegisterApproval_AdminOnlyNeeded_Approves()
    {
        var request = CreateRequest();

        var filled = request.RegisterApproval(Admin, approverIsAdminOrModerator: true, creatorGuid: null);

        filled.Should().BeTrue();
        request.Status.Should().Be(LoanRequestStatus.Approved);
        request.AdminApproverGuid.Should().Be(Admin);
    }

    /// <summary>
    /// Tests Register Approval – Creator Required – Needs Both Gates.
    /// </summary>
    [Fact]
    public void RegisterApproval_CreatorRequired_NeedsBothGates()
    {
        var request = CreateRequest();

        var adminFilled = request.RegisterApproval(Admin, approverIsAdminOrModerator: true, creatorGuid: Creator);
        adminFilled.Should().BeTrue();
        request.Status.Should().Be(LoanRequestStatus.Pending);

        var creatorFilled = request.RegisterApproval(Creator, approverIsAdminOrModerator: false, creatorGuid: Creator);
        creatorFilled.Should().BeTrue();
        request.Status.Should().Be(LoanRequestStatus.Approved);
        request.CreatorApproverGuid.Should().Be(Creator);
    }

    /// <summary>
    /// Tests Register Approval – Creator Is Admin – Single Approval Fills Both Gates.
    /// </summary>
    [Fact]
    public void RegisterApproval_CreatorIsAdmin_SingleApprovalApproves()
    {
        var request = CreateRequest();

        var filled = request.RegisterApproval(Creator, approverIsAdminOrModerator: true, creatorGuid: Creator);

        filled.Should().BeTrue();
        request.Status.Should().Be(LoanRequestStatus.Approved);
        request.AdminApproverGuid.Should().Be(Creator);
        request.CreatorApproverGuid.Should().Be(Creator);
    }

    /// <summary>
    /// Tests Register Approval – Second Admin After Admin Gate Filled – Fills Nothing.
    /// </summary>
    [Fact]
    public void RegisterApproval_SecondAdminWhileCreatorPending_FillsNothing()
    {
        var request = CreateRequest();
        request.RegisterApproval(Admin, approverIsAdminOrModerator: true, creatorGuid: Creator);

        var secondAdmin = Guid.NewGuid();
        var filled = request.RegisterApproval(secondAdmin, approverIsAdminOrModerator: true, creatorGuid: Creator);

        filled.Should().BeFalse();
        request.Status.Should().Be(LoanRequestStatus.Pending);
        request.AdminApproverGuid.Should().Be(Admin);
    }

    #endregion

    #region IsFullyApproved

    /// <summary>
    /// Tests Is Fully Approved – Admin Done No Creator – True.
    /// </summary>
    [Fact]
    public void IsFullyApproved_AdminDoneNoCreator_True()
    {
        var request = CreateRequest();
        request.RegisterApproval(Admin, approverIsAdminOrModerator: true, creatorGuid: null);

        request.IsFullyApproved(null).Should().BeTrue();
    }

    /// <summary>
    /// Tests Is Fully Approved – Admin Done Creator Pending – False.
    /// </summary>
    [Fact]
    public void IsFullyApproved_AdminDoneCreatorPending_False()
    {
        var request = CreateRequest();
        request.RegisterApproval(Admin, approverIsAdminOrModerator: true, creatorGuid: Creator);

        request.IsFullyApproved(Creator).Should().BeFalse();
    }

    #endregion

    #region Reject

    /// <summary>
    /// Tests Reject – Sets Rejected With Reason.
    /// </summary>
    [Fact]
    public void Reject_SetsRejectedWithReason()
    {
        var request = CreateRequest();

        request.Reject(Admin, "Not available right now.");

        request.Status.Should().Be(LoanRequestStatus.Rejected);
        request.RejectedByGuid.Should().Be(Admin);
        request.RejectionReason.Should().Be("Not available right now.");
        request.RejectedAt.Should().NotBeNull();
    }

    #endregion
}
