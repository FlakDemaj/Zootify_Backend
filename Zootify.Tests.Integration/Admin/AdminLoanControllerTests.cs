using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.DTOs.Admin.Response;
using Zootify_Backend.DTOs.Base;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Admin;

public class AdminLoanControllerTests : IntegrationTestBase
{
    public AdminLoanControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    #region GetBorrowers

    [Fact]
    public async Task GetBorrowers_ReturnsMatchingUsers()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        await UserRepo.CreateUserAsync("anna.schmidt@test.de", firstName: "Anna", lastName: "Schmidt");
        await UserRepo.CreateUserAsync("anna.meier@test.de",   firstName: "Anna", lastName: "Meier");
        await UserRepo.CreateUserAsync("bert.müller@test.de",  firstName: "Bert", lastName: "Müller");
        AuthenticateAs(admin);

        var result = await Client.GetFromJsonAsync<List<BorrowerPickerDto>>(
            $"{Routings.AdminLoanRouting}/borrowers?startsWith=Anna");

        result.Should().NotBeNull();
        result!.Should().HaveCount(2);
        result.Should().AllSatisfy(u => u.FullName.Should().StartWith("Anna"));
    }

    [Fact]
    public async Task GetBorrowers_NoMatch_ReturnsEmpty()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        await UserRepo.CreateUserAsync("bert@test.de", firstName: "Bert", lastName: "Müller");
        AuthenticateAs(admin);

        var result = await Client.GetFromJsonAsync<List<BorrowerPickerDto>>(
            $"{Routings.AdminLoanRouting}/borrowers?startsWith=Xyzzy");

        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBorrowers_ResultsOrderedByFirstNameThenLastName()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        await UserRepo.CreateUserAsync("b@test.de", firstName: "Anna", lastName: "Ziegler");
        await UserRepo.CreateUserAsync("a@test.de", firstName: "Anna", lastName: "Albert");
        AuthenticateAs(admin);

        var result = await Client.GetFromJsonAsync<List<BorrowerPickerDto>>(
            $"{Routings.AdminLoanRouting}/borrowers?startsWith=Anna");

        result.Should().NotBeNull();
        result![0].FullName.Should().Be("Anna Albert");
        result[1].FullName.Should().Be("Anna Ziegler");
    }

    [Fact]
    public async Task GetBorrowers_NormalUser_ReturnsForbidden()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        AuthenticateAs(user);

        var response = await Client.GetAsync(
            $"{Routings.AdminLoanRouting}/borrowers?startsWith=A");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetBorrowers_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync(
            $"{Routings.AdminLoanRouting}/borrowers?startsWith=A");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region CreateLoan

    [Fact]
    public async Task CreateLoan_Success_ReturnsLoanGuid()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var borrower = await UserRepo.CreateUserAsync("borrower@test.de");
        AuthenticateAs(admin);

        var geo = await SpecimenRepo.CreateGeographicLocationAsync();
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, geo.Guid);

        var request = new CreateLoanRequestDto
        {
            SpecimenGuid = specimen.Guid,
            BorrowerGuid = borrower.Guid,
            DueDate      = DateTime.UtcNow.AddDays(30),
            Notes        = "Handle with care.",
        };
        var response = await Client.PostAsJsonAsync(Routings.AdminLoanRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CreatedObjectResponseDto>();
        result!.CreatedObjectGuid.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateLoan_AllFieldsPersisted()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var borrower = await UserRepo.CreateUserAsync("borrower@test.de");
        AuthenticateAs(admin);

        var geo = await SpecimenRepo.CreateGeographicLocationAsync();
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, geo.Guid);

        var dueDate = DateTime.UtcNow.AddDays(14);
        var before = DateTime.UtcNow;

        var request = new CreateLoanRequestDto
        {
            SpecimenGuid = specimen.Guid,
            BorrowerGuid = borrower.Guid,
            DueDate      = dueDate,
            Notes        = "Fragile – do not stack.",
        };
        var response = await Client.PostAsJsonAsync(Routings.AdminLoanRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CreatedObjectResponseDto>();

        var loan = await GetLoanFromDbAsync(result!.CreatedObjectGuid);
        loan!.SpecimenGuid.Should().Be(specimen.Guid);
        loan.BorrowerGuid.Should().Be(borrower.Guid);
        loan.LenderGuid.Should().Be(admin.Guid);
        loan.LoanDate.Should().BeCloseTo(before, precision: TimeSpan.FromSeconds(5));
        loan.DueDate.Should().BeCloseTo(dueDate, precision: TimeSpan.FromSeconds(2));
        loan.Notes.Should().Be("Fragile – do not stack.");
        loan.ReturnDate.Should().BeNull();
    }

    [Fact]
    public async Task CreateLoan_Moderator_Success()
    {
        var mod = await UserRepo.CreateUserAsync("mod@test.de", role: UserRole.Moderator);
        var borrower = await UserRepo.CreateUserAsync("borrower@test.de");
        AuthenticateAs(mod);

        var geo = await SpecimenRepo.CreateGeographicLocationAsync();
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, geo.Guid);

        var request = new CreateLoanRequestDto
        {
            SpecimenGuid = specimen.Guid,
            BorrowerGuid = borrower.Guid,
            DueDate      = DateTime.UtcNow.AddDays(10),
        };
        var response = await Client.PostAsJsonAsync(Routings.AdminLoanRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateLoan_SpecimenNotFound_ReturnsError()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var borrower = await UserRepo.CreateUserAsync("borrower@test.de");
        AuthenticateAs(admin);

        var request = new CreateLoanRequestDto
        {
            SpecimenGuid = Guid.NewGuid(),
            BorrowerGuid = borrower.Guid,
            DueDate      = DateTime.UtcNow.AddDays(10),
        };
        var response = await Client.PostAsJsonAsync(Routings.AdminLoanRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoSpecimenFound);
    }

    [Fact]
    public async Task CreateLoan_BorrowerNotFound_ReturnsError()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        AuthenticateAs(admin);

        var geo = await SpecimenRepo.CreateGeographicLocationAsync();
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, geo.Guid);

        var request = new CreateLoanRequestDto
        {
            SpecimenGuid = specimen.Guid,
            BorrowerGuid = Guid.NewGuid(),
            DueDate      = DateTime.UtcNow.AddDays(10),
        };
        var response = await Client.PostAsJsonAsync(Routings.AdminLoanRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoUserFound);
    }

    [Fact]
    public async Task CreateLoan_NormalUser_ReturnsForbidden()
    {
        var user = await UserRepo.CreateUserAsync("user@test.de");
        AuthenticateAs(user);

        var request = new CreateLoanRequestDto
        {
            SpecimenGuid = Guid.NewGuid(),
            BorrowerGuid = Guid.NewGuid(),
            DueDate      = DateTime.UtcNow.AddDays(10),
        };
        var response = await Client.PostAsJsonAsync(Routings.AdminLoanRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateLoan_Unauthenticated_ReturnsUnauthorized()
    {
        var request = new CreateLoanRequestDto
        {
            SpecimenGuid = Guid.NewGuid(),
            BorrowerGuid = Guid.NewGuid(),
            DueDate      = DateTime.UtcNow.AddDays(10),
        };
        var response = await Client.PostAsJsonAsync(Routings.AdminLoanRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region ReturnLoan

    private async Task<(Guid loanGuid, UserEntity admin, UserEntity borrower)> CreateLoanInDb()
    {
        var admin    = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var borrower = await UserRepo.CreateUserAsync("borrower@test.de");
        AuthenticateAs(admin);

        var geo      = await SpecimenRepo.CreateGeographicLocationAsync();
        var species  = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, geo.Guid);

        var createRequest = new CreateLoanRequestDto
        {
            SpecimenGuid = specimen.Guid,
            BorrowerGuid = borrower.Guid,
            DueDate      = DateTime.UtcNow.AddDays(14),
            Notes        = "Original notes.",
        };
        var create = await Client.PostAsJsonAsync(Routings.AdminLoanRouting, createRequest);
        create.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await create.Content.ReadFromJsonAsync<CreatedObjectResponseDto>();

        return (result!.CreatedObjectGuid, admin, borrower);
    }

    [Fact]
    public async Task ReturnLoan_Success_SetsReturnDate()
    {
        var (loanGuid, _, _) = await CreateLoanInDb();
        var before = DateTime.UtcNow;

        var request = new ReturnLoanRequestDto();
        var response = await Client.PostAsJsonAsync(
            $"{Routings.AdminLoanRouting}/{loanGuid}/return", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var loan = await GetLoanFromDbAsync(loanGuid);
        loan!.ReturnDate.Should().NotBeNull();
        loan.ReturnDate!.Value.Should().BeCloseTo(before, precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ReturnLoan_WithAppendedNotes_AppendsToExistingNotes()
    {
        var (loanGuid, _, _) = await CreateLoanInDb();

        var request = new ReturnLoanRequestDto { AppendedNotes = "Returned in good condition." };
        var response = await Client.PostAsJsonAsync(
            $"{Routings.AdminLoanRouting}/{loanGuid}/return", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var loan = await GetLoanFromDbAsync(loanGuid);
        loan!.Notes.Should().Be("Original notes.\nReturned in good condition.");
    }

    [Fact]
    public async Task ReturnLoan_WithoutAppendedNotes_OriginalNotesUnchanged()
    {
        var (loanGuid, _, _) = await CreateLoanInDb();

        var request = new ReturnLoanRequestDto();
        var response = await Client.PostAsJsonAsync(
            $"{Routings.AdminLoanRouting}/{loanGuid}/return", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var loan = await GetLoanFromDbAsync(loanGuid);
        loan!.Notes.Should().Be("Original notes.");
    }

    [Fact]
    public async Task ReturnLoan_Moderator_Success()
    {
        var (loanGuid, _, _) = await CreateLoanInDb();

        var mod = await UserRepo.CreateUserAsync("mod@test.de", role: UserRole.Moderator);
        AuthenticateAs(mod);

        var request = new ReturnLoanRequestDto();
        var response = await Client.PostAsJsonAsync(
            $"{Routings.AdminLoanRouting}/{loanGuid}/return", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReturnLoan_AlreadyReturned_ReturnsError()
    {
        var (loanGuid, _, _) = await CreateLoanInDb();

        var firstRequest = new ReturnLoanRequestDto();
        await Client.PostAsJsonAsync(
            $"{Routings.AdminLoanRouting}/{loanGuid}/return", firstRequest);

        var secondRequest = new ReturnLoanRequestDto();
        var response = await Client.PostAsJsonAsync(
            $"{Routings.AdminLoanRouting}/{loanGuid}/return", secondRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.LoanAlreadyReturned);
    }

    [Fact]
    public async Task ReturnLoan_NotFound_ReturnsError()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        AuthenticateAs(admin);

        var request = new ReturnLoanRequestDto();
        var response = await Client.PostAsJsonAsync(
            $"{Routings.AdminLoanRouting}/{Guid.NewGuid()}/return", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.LoanNotFound);
    }

    [Fact]
    public async Task ReturnLoan_NormalUser_ReturnsForbidden()
    {
        var (loanGuid, _, _) = await CreateLoanInDb();

        var user = await UserRepo.CreateUserAsync("user@test.de");
        AuthenticateAs(user);

        var request = new ReturnLoanRequestDto();
        var response = await Client.PostAsJsonAsync(
            $"{Routings.AdminLoanRouting}/{loanGuid}/return", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReturnLoan_Unauthenticated_ReturnsUnauthorized()
    {
        Client.DefaultRequestHeaders.Clear();

        var request = new ReturnLoanRequestDto();
        var response = await Client.PostAsJsonAsync(
            $"{Routings.AdminLoanRouting}/{Guid.NewGuid()}/return", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}
