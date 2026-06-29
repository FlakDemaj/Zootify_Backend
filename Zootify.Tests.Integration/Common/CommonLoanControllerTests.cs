using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Response;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Common;

/// <summary>
/// Tests for Common Loan Controller — active-loan reads (list + detail) and a borrower's own loans/return.
/// </summary>
public class CommonLoanControllerTests : IntegrationTestBase
{
    public CommonLoanControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    private async Task<(UserEntity admin, SpecimenEntity specimen)> ArrangeAsync()
    {
        var admin    = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var geo      = await SpecimenRepo.CreateGeographicLocationAsync();
        var species  = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, geo.Guid, name: "Lion");
        return (admin, specimen);
    }

    #region GetBorrowed

    /// <summary>
    /// Tests Get Borrowed – Returns Only The Caller's Active Loans.
    /// </summary>
    [Fact]
    public async Task GetBorrowed_ReturnsOnlyOwnActiveLoans()
    {
        var (admin, specimen) = await ArrangeAsync();
        var user  = await UserRepo.CreateUserAsync("user@test.de");
        var other = await UserRepo.CreateUserAsync("other@test.de");

        var ownLoan = await LoanRepo.CreateLoanAsync(specimen.Guid, admin.Guid, user.Guid, DateTime.UtcNow.AddDays(14));
        await LoanRepo.CreateLoanAsync(specimen.Guid, admin.Guid, other.Guid, DateTime.UtcNow.AddDays(14));
        AuthenticateAs(user);

        var result = await Client.GetFromJsonAsync<List<LoanOverviewDto>>(Routings.CommonLoanBorrowedRouting);

        result.Should().NotBeNull();
        result!.Should().ContainSingle(l => l.LoanGuid == ownLoan.Guid);
    }

    /// <summary>
    /// Tests Get Borrowed – Excludes Already Returned Loans.
    /// </summary>
    [Fact]
    public async Task GetBorrowed_ExcludesReturnedLoans()
    {
        var (admin, specimen) = await ArrangeAsync();
        var user = await UserRepo.CreateUserAsync("user@test.de");
        await LoanRepo.CreateLoanAsync(
            specimen.Guid, admin.Guid, user.Guid, DateTime.UtcNow.AddDays(14), returnDate: DateTime.UtcNow);
        AuthenticateAs(user);

        var result = await Client.GetFromJsonAsync<List<LoanOverviewDto>>(Routings.CommonLoanBorrowedRouting);

        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    /// <summary>
    /// Tests Get Borrowed – Unauthenticated – Returns Unauthorized.
    /// </summary>
    [Fact]
    public async Task GetBorrowed_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync(Routings.CommonLoanBorrowedRouting);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region ReturnOwnLoan

    /// <summary>
    /// Tests Return Own Loan – Success – Sets Return Date.
    /// </summary>
    [Fact]
    public async Task ReturnOwnLoan_Success_SetsReturnDate()
    {
        var (admin, specimen) = await ArrangeAsync();
        var user = await UserRepo.CreateUserAsync("user@test.de");
        var loan = await LoanRepo.CreateLoanAsync(specimen.Guid, admin.Guid, user.Guid, DateTime.UtcNow.AddDays(14));
        AuthenticateAs(user);
        var before = DateTime.UtcNow;

        var response = await Client.PostAsync($"{Routings.CommonLoanRouting}/{loan.Guid}/return", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await GetLoanFromDbAsync(loan.Guid);
        stored!.ReturnDate.Should().NotBeNull();
        stored.ReturnDate!.Value.Should().BeCloseTo(before, precision: TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Tests Return Own Loan – Email Enabled – Sends Confirmation Email To Borrower.
    /// </summary>
    [Fact]
    public async Task ReturnOwnLoan_EmailEnabled_SendsConfirmationEmail()
    {
        Fixture.BrevoMock
            .Setup(b => b.SendLoanReturnConfirmationEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(true);

        var (admin, specimen) = await ArrangeAsync();
        var user = await UserRepo.CreateUserAsync("user@test.de");
        var loan = await LoanRepo.CreateLoanAsync(specimen.Guid, admin.Guid, user.Guid, DateTime.UtcNow.AddDays(14));
        AuthenticateAs(user);

        var response = await Client.PostAsync($"{Routings.CommonLoanRouting}/{loan.Guid}/return", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Fixture.BrevoMock.Verify(b => b.SendLoanReturnConfirmationEmail(
            "user@test.de", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    /// <summary>
    /// Tests Return Own Loan – Email Disabled – Does Not Send Confirmation Email.
    /// </summary>
    [Fact]
    public async Task ReturnOwnLoan_EmailDisabled_DoesNotSendEmail()
    {
        var (admin, specimen) = await ArrangeAsync();
        var user = await UserRepo.CreateUserAsync("user@test.de");
        await CreateUserSettingsInDbAsync(user.Guid, sendEmail: false);
        var loan = await LoanRepo.CreateLoanAsync(specimen.Guid, admin.Guid, user.Guid, DateTime.UtcNow.AddDays(14));
        AuthenticateAs(user);

        var response = await Client.PostAsync($"{Routings.CommonLoanRouting}/{loan.Guid}/return", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Fixture.BrevoMock.Verify(b => b.SendLoanReturnConfirmationEmail(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
    }

    /// <summary>
    /// Tests Return Own Loan – Not The Borrower – Returns Error.
    /// </summary>
    [Fact]
    public async Task ReturnOwnLoan_NotBorrower_ReturnsError()
    {
        var (admin, specimen) = await ArrangeAsync();
        var borrower = await UserRepo.CreateUserAsync("borrower@test.de");
        var other    = await UserRepo.CreateUserAsync("other@test.de");
        var loan = await LoanRepo.CreateLoanAsync(specimen.Guid, admin.Guid, borrower.Guid, DateTime.UtcNow.AddDays(14));
        AuthenticateAs(other);

        var response = await Client.PostAsync($"{Routings.CommonLoanRouting}/{loan.Guid}/return", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.LoanNotFound);
    }

    /// <summary>
    /// Tests Return Own Loan – Already Returned – Returns Error.
    /// </summary>
    [Fact]
    public async Task ReturnOwnLoan_AlreadyReturned_ReturnsError()
    {
        var (admin, specimen) = await ArrangeAsync();
        var user = await UserRepo.CreateUserAsync("user@test.de");
        var loan = await LoanRepo.CreateLoanAsync(
            specimen.Guid, admin.Guid, user.Guid, DateTime.UtcNow.AddDays(14), returnDate: DateTime.UtcNow);
        AuthenticateAs(user);

        var response = await Client.PostAsync($"{Routings.CommonLoanRouting}/{loan.Guid}/return", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.LoanNotFound);
    }

    /// <summary>
    /// Tests Return Own Loan – Unauthenticated – Returns Unauthorized.
    /// </summary>
    [Fact]
    public async Task ReturnOwnLoan_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await Client.PostAsync($"{Routings.CommonLoanRouting}/{Guid.NewGuid()}/return", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GetActiveLoans

    private async Task<(UserEntity admin, UserEntity borrower, SpecimenEntity specimen)> CreateLoanFixtureAsync()
    {
        var admin    = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var borrower = await UserRepo.CreateUserAsync("borrower@test.de", firstName: "Anna", lastName: "Schmidt");

        var geo      = await SpecimenRepo.CreateGeographicLocationAsync();
        var species  = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, geo.Guid, name: "Lion");

        return (admin, borrower, specimen);
    }

    /// <summary>
    /// Tests Get Active Loans – Returns Only Active Loans.
    /// </summary>
    [Fact]
    public async Task GetActiveLoans_ReturnsOnlyActiveLoans()
    {
        var (admin, borrower, specimen) = await CreateLoanFixtureAsync();
        var activeLoan = await LoanRepo.CreateLoanAsync(
            specimen.Guid, admin.Guid, borrower.Guid, DateTime.UtcNow.AddDays(14));
        await LoanRepo.CreateLoanAsync(
            specimen.Guid, admin.Guid, borrower.Guid, DateTime.UtcNow.AddDays(7),
            returnDate: DateTime.UtcNow);
        AuthenticateAs(admin);

        var result = await Client.GetFromJsonAsync<GetActiveLoansPagedResponseDto>(
            Routings.CommonLoanActiveRouting);

        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(1);
        result.Loans.Should().ContainSingle(l => l.LoanGuid == activeLoan.Guid);
    }

    /// <summary>
    /// Tests Get Active Loans – Returns Expected Fields.
    /// </summary>
    [Fact]
    public async Task GetActiveLoans_ReturnsExpectedFields()
    {
        var (admin, borrower, specimen) = await CreateLoanFixtureAsync();
        var dueDate = DateTime.UtcNow.AddDays(14);
        var loan = await LoanRepo.CreateLoanAsync(
            specimen.Guid, admin.Guid, borrower.Guid, dueDate);
        AuthenticateAs(admin);

        var result = await Client.GetFromJsonAsync<GetActiveLoansPagedResponseDto>(
            Routings.CommonLoanActiveRouting);

        result.Should().NotBeNull();
        var entry = result!.Loans.Should().ContainSingle().Subject;
        entry.LoanGuid.Should().Be(loan.Guid);
        entry.SpecimenName.Should().Be("Lion");
        entry.SpecimenLatinName.Should().Be("Testus specienus");
        entry.BorrowerGuid.Should().Be(borrower.Guid);
        entry.BorrowerName.Should().Be("Anna Schmidt");
        entry.DueDate.Should().BeCloseTo(dueDate, precision: TimeSpan.FromSeconds(2));
        entry.ImageGuid.Should().Be(Guid.Empty);
    }

    /// <summary>
    /// Tests Get Active Loans – Specimen With Image – Returns Its Image Guid.
    /// </summary>
    [Fact]
    public async Task GetActiveLoans_SpecimenWithImage_ReturnsImageGuid()
    {
        var admin    = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var borrower = await UserRepo.CreateUserAsync("borrower@test.de");
        var geo      = await SpecimenRepo.CreateGeographicLocationAsync();
        var species  = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var specimen = await SpecimenRepo.CreateSpecimenWithImageAsync(species.Guid, geo.Guid);
        var image    = (await GetImagesForSpecimenFromDbAsync(specimen.Guid)).Single();
        await LoanRepo.CreateLoanAsync(
            specimen.Guid, admin.Guid, borrower.Guid, DateTime.UtcNow.AddDays(14));
        AuthenticateAs(admin);

        var result = await Client.GetFromJsonAsync<GetActiveLoansPagedResponseDto>(
            Routings.CommonLoanActiveRouting);

        result.Should().NotBeNull();
        result!.Loans.Should().ContainSingle()
            .Which.ImageGuid.Should().Be(image.Guid);
    }

    /// <summary>
    /// Tests Get Active Loans – No Loans – Returns Empty.
    /// </summary>
    [Fact]
    public async Task GetActiveLoans_NoLoans_ReturnsEmpty()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        AuthenticateAs(admin);

        var result = await Client.GetFromJsonAsync<GetActiveLoansPagedResponseDto>(
            Routings.CommonLoanActiveRouting);

        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(0);
        result.Loans.Should().BeEmpty();
    }

    /// <summary>
    /// Tests Get Active Loans – Moderator – Success.
    /// </summary>
    [Fact]
    public async Task GetActiveLoans_Moderator_Success()
    {
        var (admin, borrower, specimen) = await CreateLoanFixtureAsync();
        await LoanRepo.CreateLoanAsync(
            specimen.Guid, admin.Guid, borrower.Guid, DateTime.UtcNow.AddDays(14));

        var mod = await UserRepo.CreateUserAsync("mod@test.de", role: UserRole.Moderator);
        AuthenticateAs(mod);

        var response = await Client.GetAsync(Routings.CommonLoanActiveRouting);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetActiveLoansPagedResponseDto>();
        result!.TotalCount.Should().Be(1);
    }

    /// <summary>
    /// Tests Get Active Loans – Paging – Returns Requested Page Size And Full Total.
    /// </summary>
    [Fact]
    public async Task GetActiveLoans_Paging_ReturnsCorrectPage()
    {
        var (admin, borrower, specimen) = await CreateLoanFixtureAsync();
        for (var i = 0; i < 5; i++)
        {
            await LoanRepo.CreateLoanAsync(
                specimen.Guid, admin.Guid, borrower.Guid, DateTime.UtcNow.AddDays(i + 1));
        }
        AuthenticateAs(admin);

        var result = await Client.GetFromJsonAsync<GetActiveLoansPagedResponseDto>(
            $"{Routings.CommonLoanActiveRouting}?page=1&pageSize=2");

        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(5);
        result.Loans.Should().HaveCount(2);
    }

    /// <summary>
    /// Tests Get Active Loans – Default Sort – Orders By Due Date Ascending.
    /// </summary>
    [Fact]
    public async Task GetActiveLoans_DefaultSort_OrdersByDueDateAscending()
    {
        var (admin, borrower, specimen) = await CreateLoanFixtureAsync();
        var later   = await LoanRepo.CreateLoanAsync(
            specimen.Guid, admin.Guid, borrower.Guid, DateTime.UtcNow.AddDays(30));
        var earlier = await LoanRepo.CreateLoanAsync(
            specimen.Guid, admin.Guid, borrower.Guid, DateTime.UtcNow.AddDays(5));
        AuthenticateAs(admin);

        var result = await Client.GetFromJsonAsync<GetActiveLoansPagedResponseDto>(
            Routings.CommonLoanActiveRouting);

        result.Should().NotBeNull();
        result!.Loans[0].LoanGuid.Should().Be(earlier.Guid);
        result.Loans[1].LoanGuid.Should().Be(later.Guid);
    }

    /// <summary>
    /// Tests Get Active Loans – Sort By Due Date Descending.
    /// </summary>
    [Fact]
    public async Task GetActiveLoans_SortByDueDateDescending()
    {
        var (admin, borrower, specimen) = await CreateLoanFixtureAsync();
        var later   = await LoanRepo.CreateLoanAsync(
            specimen.Guid, admin.Guid, borrower.Guid, DateTime.UtcNow.AddDays(30));
        var earlier = await LoanRepo.CreateLoanAsync(
            specimen.Guid, admin.Guid, borrower.Guid, DateTime.UtcNow.AddDays(5));
        AuthenticateAs(admin);

        var result = await Client.GetFromJsonAsync<GetActiveLoansPagedResponseDto>(
            $"{Routings.CommonLoanActiveRouting}?sortBy=dueDate&sortDirection={SortDirection.Desc}");

        result.Should().NotBeNull();
        result!.Loans[0].LoanGuid.Should().Be(later.Guid);
        result.Loans[1].LoanGuid.Should().Be(earlier.Guid);
    }

    /// <summary>
    /// Tests Get Active Loans – Normal User – Sees Only Loans Of Specimens They Created.
    /// </summary>
    [Fact]
    public async Task GetActiveLoans_NormalUser_SeesOnlyOwnCreatedSpecimenLoans()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var other = await UserRepo.CreateUserAsync("other@test.de");

        var geo     = await SpecimenRepo.CreateGeographicLocationAsync();
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var ownSpecimen   = await SpecimenRepo.CreateSpecimenAsync(species.Guid, geo.Guid, createdByUserGuid: owner.Guid);
        var otherSpecimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, geo.Guid, createdByUserGuid: other.Guid);

        var ownLoan = await LoanRepo.CreateLoanAsync(
            ownSpecimen.Guid, admin.Guid, other.Guid, DateTime.UtcNow.AddDays(14));
        await LoanRepo.CreateLoanAsync(
            otherSpecimen.Guid, admin.Guid, owner.Guid, DateTime.UtcNow.AddDays(14));
        AuthenticateAs(owner);

        var result = await Client.GetFromJsonAsync<GetActiveLoansPagedResponseDto>(
            Routings.CommonLoanActiveRouting);

        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(1);
        result.Loans.Should().ContainSingle(l => l.LoanGuid == ownLoan.Guid);
    }

    /// <summary>
    /// Tests Get Active Loans – Admin – Sees Loans Regardless Of Specimen Owner.
    /// </summary>
    [Fact]
    public async Task GetActiveLoans_Admin_SeesLoansRegardlessOfOwner()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var other = await UserRepo.CreateUserAsync("other@test.de");

        var geo     = await SpecimenRepo.CreateGeographicLocationAsync();
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var ownerSpecimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, geo.Guid, createdByUserGuid: owner.Guid);
        var otherSpecimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, geo.Guid, createdByUserGuid: other.Guid);

        await LoanRepo.CreateLoanAsync(ownerSpecimen.Guid, admin.Guid, other.Guid, DateTime.UtcNow.AddDays(14));
        await LoanRepo.CreateLoanAsync(otherSpecimen.Guid, admin.Guid, owner.Guid, DateTime.UtcNow.AddDays(14));
        AuthenticateAs(admin);

        var result = await Client.GetFromJsonAsync<GetActiveLoansPagedResponseDto>(
            Routings.CommonLoanActiveRouting);

        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(2);
    }

    /// <summary>
    /// Tests Get Active Loans – Unauthenticated – Returns Unauthorized.
    /// </summary>
    [Fact]
    public async Task GetActiveLoans_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync(Routings.CommonLoanActiveRouting);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GetActiveLoanDetail

    /// <summary>
    /// Tests Get Active Loan Detail – Returns Expected Fields.
    /// </summary>
    [Fact]
    public async Task GetActiveLoanDetail_ReturnsExpectedFields()
    {
        var admin    = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin,
            firstName: "Max", lastName: "Mustermann");
        var borrower = await UserRepo.CreateUserAsync("borrower@test.de",
            firstName: "Anna", lastName: "Schmidt");
        var geo      = await SpecimenRepo.CreateGeographicLocationAsync();
        var species  = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, geo.Guid, name: "Lion");
        var dueDate  = DateTime.UtcNow.AddDays(14);
        var loan     = await LoanRepo.CreateLoanAsync(
            specimen.Guid, admin.Guid, borrower.Guid, dueDate, notes: "Handle with care.");
        AuthenticateAs(admin);

        var result = await Client.GetFromJsonAsync<LoanDetailDto>(
            $"{Routings.CommonLoanRouting}/{loan.Guid}");

        result.Should().NotBeNull();
        result!.LoanGuid.Should().Be(loan.Guid);
        result.SpecimenGuid.Should().Be(specimen.Guid);
        result.SpecimenName.Should().Be("Lion");
        result.SpecimenLatinName.Should().Be("Testus specienus");
        result.ImageGuid.Should().BeNull();
        result.BorrowerGuid.Should().Be(borrower.Guid);
        result.BorrowerName.Should().Be("Anna Schmidt");
        result.BorrowerEmail.Should().Be("borrower@test.de");
        result.LenderGuid.Should().Be(admin.Guid);
        result.LenderName.Should().Be("Max Mustermann");
        result.DueDate.Should().BeCloseTo(dueDate, precision: TimeSpan.FromSeconds(2));
        result.Notes.Should().Be("Handle with care.");
    }

    /// <summary>
    /// Tests Get Active Loan Detail – Specimen With Image – Returns Its Image Guid.
    /// </summary>
    [Fact]
    public async Task GetActiveLoanDetail_SpecimenWithImage_ReturnsImageGuid()
    {
        var admin    = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var borrower = await UserRepo.CreateUserAsync("borrower@test.de");
        var geo      = await SpecimenRepo.CreateGeographicLocationAsync();
        var species  = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var specimen = await SpecimenRepo.CreateSpecimenWithImageAsync(species.Guid, geo.Guid);
        var image    = (await GetImagesForSpecimenFromDbAsync(specimen.Guid)).Single();
        var loan     = await LoanRepo.CreateLoanAsync(
            specimen.Guid, admin.Guid, borrower.Guid, DateTime.UtcNow.AddDays(14));
        AuthenticateAs(admin);

        var result = await Client.GetFromJsonAsync<LoanDetailDto>(
            $"{Routings.CommonLoanRouting}/{loan.Guid}");

        result.Should().NotBeNull();
        result!.ImageGuid.Should().Be(image.Guid);
    }

    /// <summary>
    /// Tests Get Active Loan Detail – Moderator – Success.
    /// </summary>
    [Fact]
    public async Task GetActiveLoanDetail_Moderator_Success()
    {
        var admin    = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var borrower = await UserRepo.CreateUserAsync("borrower@test.de");
        var geo      = await SpecimenRepo.CreateGeographicLocationAsync();
        var species  = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, geo.Guid);
        var loan     = await LoanRepo.CreateLoanAsync(
            specimen.Guid, admin.Guid, borrower.Guid, DateTime.UtcNow.AddDays(14));

        var mod = await UserRepo.CreateUserAsync("mod@test.de", role: UserRole.Moderator);
        AuthenticateAs(mod);

        var response = await Client.GetAsync($"{Routings.CommonLoanRouting}/{loan.Guid}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Tests Get Active Loan Detail – Returned Loan – Returns Error.
    /// </summary>
    [Fact]
    public async Task GetActiveLoanDetail_ReturnedLoan_ReturnsError()
    {
        var admin    = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var borrower = await UserRepo.CreateUserAsync("borrower@test.de");
        var geo      = await SpecimenRepo.CreateGeographicLocationAsync();
        var species  = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, geo.Guid);
        var loan     = await LoanRepo.CreateLoanAsync(
            specimen.Guid, admin.Guid, borrower.Guid, DateTime.UtcNow.AddDays(14),
            returnDate: DateTime.UtcNow);
        AuthenticateAs(admin);

        var response = await Client.GetAsync($"{Routings.CommonLoanRouting}/{loan.Guid}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.LoanNotFound);
    }

    /// <summary>
    /// Tests Get Active Loan Detail – Not Found – Returns Error.
    /// </summary>
    [Fact]
    public async Task GetActiveLoanDetail_NotFound_ReturnsError()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        AuthenticateAs(admin);

        var response = await Client.GetAsync($"{Routings.CommonLoanRouting}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.LoanNotFound);
    }

    /// <summary>
    /// Tests Get Active Loan Detail – Normal User – Own Created Specimen – Success.
    /// </summary>
    [Fact]
    public async Task GetActiveLoanDetail_NormalUser_OwnCreatedSpecimen_Success()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var owner = await UserRepo.CreateUserAsync("owner@test.de");

        var geo      = await SpecimenRepo.CreateGeographicLocationAsync();
        var species  = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, geo.Guid, createdByUserGuid: owner.Guid);
        var loan     = await LoanRepo.CreateLoanAsync(
            specimen.Guid, admin.Guid, admin.Guid, DateTime.UtcNow.AddDays(14));
        AuthenticateAs(owner);

        var result = await Client.GetFromJsonAsync<LoanDetailDto>(
            $"{Routings.CommonLoanRouting}/{loan.Guid}");

        result.Should().NotBeNull();
        result!.LoanGuid.Should().Be(loan.Guid);
    }

    /// <summary>
    /// Tests Get Active Loan Detail – Normal User – Other User's Specimen – Returns Error.
    /// </summary>
    [Fact]
    public async Task GetActiveLoanDetail_NormalUser_OtherUsersSpecimen_ReturnsError()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var owner = await UserRepo.CreateUserAsync("owner@test.de");
        var other = await UserRepo.CreateUserAsync("other@test.de");

        var geo      = await SpecimenRepo.CreateGeographicLocationAsync();
        var species  = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, geo.Guid, createdByUserGuid: owner.Guid);
        var loan     = await LoanRepo.CreateLoanAsync(
            specimen.Guid, admin.Guid, owner.Guid, DateTime.UtcNow.AddDays(14));
        AuthenticateAs(other);

        var response = await Client.GetAsync($"{Routings.CommonLoanRouting}/{loan.Guid}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.LoanNotFound);
    }

    /// <summary>
    /// Tests Get Active Loan Detail – Unauthenticated – Returns Unauthorized.
    /// </summary>
    [Fact]
    public async Task GetActiveLoanDetail_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync($"{Routings.CommonLoanRouting}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}
