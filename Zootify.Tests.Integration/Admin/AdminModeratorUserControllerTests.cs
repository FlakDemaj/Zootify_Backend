using Microsoft.AspNetCore.WebUtilities;
using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.DTOs.Admin.Response;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Response;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Admin;

/// <summary>
/// Tests for Admin Moderator User Controller.
/// </summary>
public class AdminModeratorUserControllerTests : IntegrationTestBase
{
    public AdminModeratorUserControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    #region GetUser

    /// <summary>
    /// Tests Get User – As Admin – Returns User.
    /// </summary>
    [Fact]
    public async Task GetUser_AsAdmin_ReturnsUser()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test1@test.de",
            role: UserRole.Admin);

        var updater = await UserRepo.CreateUserAsync(
            "test2@test.de",
            firstName: "Max",
            lastName: "TestPerson");

        var deleter = await UserRepo.CreateUserAsync(
            "test3@test.de",
            firstName: "Stephan",
            lastName: "Billig");

        var userToGet = await UserRepo.CreateUserAsync(
            "test4@test.de",
            updater: updater.Guid,
            updateDateTime: DateTime.UtcNow.AddDays(-23),
            deleter: deleter.Guid,
            deletionDateTime: DateTime.UtcNow.AddDays(-5),
            role: UserRole.Admin,
            status: UserStatus.Deleted);

        AuthenticateAs(requester);

        var response = await Client.GetFromJsonAsync<GetUserResponseDto>(
            $"{Routings.AdminUserRouting}/{userToGet.Guid}");

        response.Should().NotBeNull();

        response!.FirstName.Should().Be(userToGet.FirstName);
        response.LastName.Should().Be(userToGet.LastName);
        response.Email.Should().Be(userToGet.Email);
        response.UserGuid.Should().Be(userToGet.Guid);
        response.UserRole.Should().Be(userToGet.Role);
        response.LoginAttempts.Should().Be(userToGet.LoginAttempts);
        response.UserStatus.Should().Be(userToGet.Status);

        response.CreatedAt.Should().Be(userToGet.CreationDate);

        response.UpdatedAt.Should().Be(userToGet.UpdatedDate);
        response.UpdatedBy.Should().Be(updater.FirstName + " " + updater.LastName);

        response.DeletedAt.Should().Be(userToGet.DeletionDate);
        response.DeletedBy.Should().Be(deleter.FirstName + " " + deleter.LastName);
    }
    
    /// <summary>
    /// Tests Get User – As Moderator – Returns User.
    /// </summary>
    [Fact]
    public async Task GetUser_AsModerator_ReturnsUser()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test1@test.de",
            role: UserRole.Moderator);

        var updater = await UserRepo.CreateUserAsync(
            "test2@test.de",
            firstName: "Max",
            lastName: "TestPerson");

        var deleter = await UserRepo.CreateUserAsync(
            "test3@test.de",
            firstName: "Stephan",
            lastName: "Billig");

        var userToGet = await UserRepo.CreateUserAsync(
            "test4@test.de",
            updater: updater.Guid,
            updateDateTime: DateTime.UtcNow.AddDays(-23),
            deleter: deleter.Guid,
            deletionDateTime: DateTime.UtcNow.AddDays(-5),
            role: UserRole.Admin,
            status: UserStatus.Deleted);

        AuthenticateAs(requester);

        var response = await Client.GetFromJsonAsync<GetUserResponseDto>(
            $"{Routings.AdminUserRouting}/{userToGet.Guid}");

        response.Should().NotBeNull();

        response!.FirstName.Should().Be(userToGet.FirstName);
        response.LastName.Should().Be(userToGet.LastName);
        response.Email.Should().Be(userToGet.Email);
        response.UserGuid.Should().Be(userToGet.Guid);
        response.UserRole.Should().Be(userToGet.Role);
        response.LoginAttempts.Should().Be(userToGet.LoginAttempts);
        response.UserStatus.Should().Be(userToGet.Status);

        response.CreatedAt.Should().Be(userToGet.CreationDate);

        response.UpdatedAt.Should().Be(userToGet.UpdatedDate);
        response.UpdatedBy.Should().Be(updater.FirstName + " " + updater.LastName);

        response.DeletedAt.Should().Be(userToGet.DeletionDate);
        response.DeletedBy.Should().Be(deleter.FirstName + " " + deleter.LastName);
    }

    /// <summary>
    /// Tests Get User – Valid Guid – Updater Is Null.
    /// </summary>
    [Fact]
    public async Task GetUser_ValidGuid_UpdaterIsNull()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test1@test.de",
            role: UserRole.Admin);

        var deleter = await UserRepo.CreateUserAsync(
            "test3@test.de",
            firstName: "Stephan",
            lastName: "Billig");

        var userToGet = await UserRepo.CreateUserAsync(
            "test4@test.de",
            updateDateTime: DateTime.UtcNow.AddDays(-23),
            deleter: deleter.Guid,
            deletionDateTime: DateTime.UtcNow.AddDays(-5),
            role: UserRole.Admin,
            status: UserStatus.Deleted);

        AuthenticateAs(requester);

        var response = await Client.GetFromJsonAsync<GetUserResponseDto>(
            $"{Routings.AdminUserRouting}/{userToGet.Guid}");

        response.Should().NotBeNull();

        response!.FirstName.Should().Be(userToGet.FirstName);
        response.LastName.Should().Be(userToGet.LastName);
        response.Email.Should().Be(userToGet.Email);
        response.UserGuid.Should().Be(userToGet.Guid);
        response.UserRole.Should().Be(userToGet.Role);
        response.LoginAttempts.Should().Be(userToGet.LoginAttempts);
        response.UserStatus.Should().Be(userToGet.Status);

        response.CreatedAt.Should().Be(userToGet.CreationDate);

        response.UpdatedAt.Should().Be(userToGet.UpdatedDate);
        response.UpdatedBy.Should().Be("Deleted User");

        response.DeletedAt.Should().Be(userToGet.DeletionDate);
        response.DeletedBy.Should().Be(deleter.FirstName + " " + deleter.LastName);
    }

    /// <summary>
    /// Tests Get User – Valid Guid – Updater And Deleter Is Null.
    /// </summary>
    [Fact]
    public async Task GetUser_ValidGuid_UpdaterAndDeleterIsNull()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test1@test.de",
            role: UserRole.Admin);

        var userToGet = await UserRepo.CreateUserAsync(
            "test4@test.de",
            updateDateTime: DateTime.UtcNow.AddDays(-23),
            deletionDateTime: DateTime.UtcNow.AddDays(-5),
            role: UserRole.Admin,
            status: UserStatus.Deleted);

        AuthenticateAs(requester);

        var response = await Client.GetFromJsonAsync<GetUserResponseDto>(
            $"{Routings.AdminUserRouting}/{userToGet.Guid}");

        response.Should().NotBeNull();

        response!.FirstName.Should().Be(userToGet.FirstName);
        response.LastName.Should().Be(userToGet.LastName);
        response.Email.Should().Be(userToGet.Email);
        response.UserGuid.Should().Be(userToGet.Guid);
        response.UserRole.Should().Be(userToGet.Role);
        response.LoginAttempts.Should().Be(userToGet.LoginAttempts);
        response.UserStatus.Should().Be(userToGet.Status);

        response.CreatedAt.Should().Be(userToGet.CreationDate);

        response.UpdatedAt.Should().Be(userToGet.UpdatedDate);
        response.UpdatedBy.Should().Be("Deleted User");

        response.DeletedAt.Should().Be(userToGet.DeletionDate);
        response.DeletedBy.Should().Be("Deleted User");
    }

    /// <summary>
    /// Tests Get User – Valid Guid – Updater Is Him Self.
    /// </summary>
    [Fact]
    public async Task GetUser_ValidGuid_UpdaterIsHimSelf()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test1@test.de",
            role: UserRole.Admin);

        var userToGet = await UserRepo.CreateUserAsync(
            "test4@test.de",
            updater: requester.Guid,
            updateDateTime: DateTime.UtcNow.AddDays(-23),
            deletionDateTime: DateTime.UtcNow.AddDays(-5),
            role: UserRole.Admin,
            status: UserStatus.Deleted);

        AuthenticateAs(requester);

        var response = await Client.GetFromJsonAsync<GetUserResponseDto>(
            $"{Routings.AdminUserRouting}/{userToGet.Guid}");

        response.Should().NotBeNull();

        response!.FirstName.Should().Be(userToGet.FirstName);
        response.LastName.Should().Be(userToGet.LastName);
        response.Email.Should().Be(userToGet.Email);
        response.UserGuid.Should().Be(userToGet.Guid);
        response.UserRole.Should().Be(userToGet.Role);
        response.LoginAttempts.Should().Be(userToGet.LoginAttempts);
        response.UserStatus.Should().Be(userToGet.Status);

        response.CreatedAt.Should().Be(userToGet.CreationDate);

        response.UpdatedAt.Should().Be(userToGet.UpdatedDate);
        response.UpdatedBy.Should().Be(requester.FirstName + " " + requester.LastName);

        response.DeletedAt.Should().Be(userToGet.DeletionDate);
        response.DeletedBy.Should().Be("Deleted User");
    }

    /// <summary>
    /// Tests Get User – Invalid Guid – Returns No User.
    /// </summary>
    [Fact]
    public async Task GetUser_InvalidGuid_ReturnsNoUser()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test1@test.de",
            role: UserRole.Admin);

        AuthenticateAs(requester);

        var response = await Client.GetAsync(
            $"{Routings.AdminUserRouting}/{Guid.CreateVersion7()}");

        var error = await ReadErrorAsync(response);

        error.Should().NotBeNull();
        error.ErrorCode.Should().Be((int)ErrorCode.NoUserFound);
    }

    /// <summary>
    /// Tests Get User – User Role – Returns Forbidden.
    /// </summary>
    [Fact]
    public async Task GetUser_UserRole_ReturnsForbidden()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test1@test.de",
            role: UserRole.User);

        AuthenticateAs(requester);

        var response = await Client.GetAsync(
            $"{Routings.AdminUserRouting}/{Guid.CreateVersion7()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GetAllUsers

    /// <summary>
    /// Tests Get All Users – Valid Admin – Returns All Users.
    /// </summary>
    [Fact]
    public async Task GetAllUsers_ValidAdmin_ReturnsAllUsers()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test1@test.de",
            role: UserRole.Admin);

        var user2 = await UserRepo.CreateUserAsync(
            "test2@test.de",
            firstName: "Hans",
            lastName: "Müller",
            role: UserRole.Moderator);

        await UserRepo.CreateUserAsync(
            "test3@test.de",
            firstName: "Anna",
            lastName: "Schmidt",
            role: UserRole.User);

        AuthenticateAs(requester);

        var response = await Client
            .GetFromJsonAsync<List<GetAllUserResponseDto>>(
                Routings.AdminUserRouting);

        response.Should().NotBeNull();
        response!.Should().HaveCount(3);

        var returnedUser2 = response!.First(u => u.Guid == user2.Guid);
        returnedUser2.FirstName.Should().Be(user2.FirstName);
        returnedUser2.LastName.Should().Be(user2.LastName);
        returnedUser2.Email.Should().Be(user2.Email);
        returnedUser2.Role.Should().Be(user2.Role);
        returnedUser2.Status.Should().Be(user2.Status);
    }

    /// <summary>
    /// Tests Get All Users – With Paging – Returns All Users.
    /// </summary>
    [Fact]
    public async Task GetAllUsers_WithPaging_ReturnsAllUsers()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test@test.de",
            role: UserRole.Admin);

        var numberOfUsers = 39;
        for (var i = 0; i <= numberOfUsers; i++)
        {
            await UserRepo.CreateUserAsync(
                $"test{i}@test.de");
        }

        AuthenticateAs(requester);

        var url = CreateQueryFilterUrl(1, 20);

        var response1 = await Client
            .GetFromJsonAsync<List<GetAllUserResponseDto>>(url);

        response1.Should().NotBeNull();
        response1!.Should().HaveCount(20);

        url = CreateQueryFilterUrl(2, 20);

        var response2 = await Client
            .GetFromJsonAsync<List<GetAllUserResponseDto>>(url);

        response2.Should().NotBeNull();
        response2!.Should().HaveCount(20);

        response1!.Intersect(response2!).Should().BeEmpty();

        url = CreateQueryFilterUrl(3, 20);

        var response3 = await Client
            .GetFromJsonAsync<List<GetAllUserResponseDto>>(url);

        response3.Should().NotBeNull();
        response3.Should().HaveCount(1);

        response3!.Intersect(response2!).Should().BeEmpty();
    }

    /// <summary>
    /// Tests Get All Users – With Filter – Returns Two Users.
    /// </summary>
    [Fact]
    public async Task GetAllUsers_WithFilter_ReturnsTwoUsers()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test@test.de",
            role: UserRole.Admin);

        var user2 = await UserRepo.CreateUserAsync(
            "test2@test.de",
            firstName: "Hans",
            lastName: "Müller",
            role: UserRole.Moderator);

        await UserRepo.CreateUserAsync(
            "test3@test.de",
            firstName: "Anna",
            lastName: "Schmidt",
            role: UserRole.User);

        var user3 = await UserRepo.CreateUserAsync(
            "test4@test.de",
            firstName: "Hanzi",
            lastName: "Eberhart",
            role: UserRole.User);

        AuthenticateAs(requester);

        var url = CreateQueryFilterUrl(
            1,
            20,
            filterParameter: "firstName",
            filter: "Han");

        var response = await Client
            .GetFromJsonAsync<List<GetAllUserResponseDto>>(url);

        response.Should().NotBeNull();
        response!.Should().HaveCount(2);

        response!.Where(u => u.Guid == user2.Guid).Should().NotBeEmpty();
        response!.Where(u => u.Guid == user3.Guid).Should().NotBeEmpty();
    }

    /// <summary>
    /// Tests Get All Users – With Sort – Returns Sorted Users.
    /// </summary>
    [Fact]
    public async Task GetAllUsers_WithSort_ReturnsSortedUsers()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test@test.de",
            role: UserRole.Admin);

        var user1 = await UserRepo.CreateUserAsync(
            "test1@test.de",
            firstName: "Hans",
            lastName: "Müller",
            status: UserStatus.Deleted,
            role: UserRole.Moderator);

        var user2 = await UserRepo.CreateUserAsync(
            "test2@test.de",
            firstName: "Anna",
            lastName: "Schmidt",
            status: UserStatus.Pending,
            role: UserRole.User);

        var user3 = await UserRepo.CreateUserAsync(
            "test3@test.de",
            firstName: "Hanzi",
            lastName: "Eberhart",
            status: UserStatus.Blocked,
            role: UserRole.User);

        AuthenticateAs(requester);

        var url = CreateQueryFilterUrl(
            1,
            20,
            sortBy: "status",
            sortDirection: SortDirection.Desc);

        var response = await Client
            .GetFromJsonAsync<List<GetAllUserResponseDto>>(url);

        response.Should().NotBeNull();
        response!.Should().HaveCount(4);

        response!.FindIndex(u => u.Guid == user2.Guid).Should().Be(3);
        response.FindIndex(u => u.Guid == user1.Guid).Should().Be(0);
        response.FindIndex(u => u.Guid == user3.Guid).Should().Be(1);
        response.FindIndex(u => u.Guid == requester.Guid).Should().Be(2);
    }

    /// <summary>
    /// Tests Get All Users – With Filter Sort And Paging – Returns Correct Slice.
    /// </summary>
    [Fact]
    public async Task GetAllUsers_WithFilterSortAndPaging_ReturnsCorrectSlice()
    {
        var requester = await UserRepo.CreateUserAsync(
            "admin@test.de",
            role: UserRole.Admin);

        var matchingUsers = new List<UserEntity>();
        for (var i = 0; i < 10; i++)
        {
            var user = await UserRepo.CreateUserAsync(
                $"hans{i}@test.de",
                firstName: "Hans");
            matchingUsers.Add(user);
        }

        for (var i = 0; i < 5; i++)
        {
            await UserRepo.CreateUserAsync(
                $"anna{i}@test.de",
                firstName: "Anna");
        }

        AuthenticateAs(requester);

        var url = CreateQueryFilterUrl(
            page: 2,
            pageSize: 5,
            sortBy: "email",
            sortDirection: SortDirection.Desc,
            filterParameter: "firstName",
            filter: "Ha");

        var response = await Client
            .GetFromJsonAsync<List<GetAllUserResponseDto>>(url);

        var expectedEmails = matchingUsers
            .OrderByDescending(u => u.Email)
            .Skip(5)
            .Take(5)
            .Select(u => u.Email)
            .ToList();

        response.Should().NotBeNull();
        response!.Select(u => u.Email).Should().Equal(expectedEmails);
    }

    /// <summary>
    /// Tests Get All Users – User Role – Returns Forbidden.
    /// </summary>
    [Fact]
    public async Task GetAllUsers_UserRole_ReturnsForbidden()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test1@test.de",
            role: UserRole.User);

        AuthenticateAs(requester);

        var response = await Client.GetAsync(Routings.AdminUserRouting);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region UpdateUser

    /// <summary>
    /// Tests Update User – Success – All Data.
    /// </summary>
    [Fact]
    public async Task UpdateUser_Success_AllData()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test1@test.de",
            role: UserRole.Admin);

        AuthenticateAs(requester);

        var userToUpdate = await UserRepo.CreateUserAsync(
            "test@test.de",
            role: UserRole.Moderator);

        var requestDto = new UpdateUserRequestDto
        {
            FirstName = "ChangedName",
            LastName = "ChangedLastName",
            Role = UserRole.Admin
        };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.AdminUserRouting}/{userToUpdate.Guid}",
            requestDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedUser = await GetUserFromDbByGuidAsync(userToUpdate.Guid);

        updatedUser.Should().NotBeNull();
        updatedUser!.FirstName.Should().Be(requestDto.FirstName);
        updatedUser.LastName.Should().Be(requestDto.LastName);
        updatedUser.LoginAttempts.Should().Be(userToUpdate.LoginAttempts);
        updatedUser.Role.Should().Be(requestDto.Role);
        updatedUser.Status.Should().Be(userToUpdate.Status);
        updatedUser.UpdatedByUserGuid.Should().Be(requester.Guid);
        updatedUser.UpdatedDate.Should().BeCloseTo(DateTime.UtcNow,
            precision: TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Tests Update User – Success – Just First Name.
    /// </summary>
    [Fact]
    public async Task UpdateUser_Success_JustFirstName()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test1@test.de",
            role: UserRole.Admin);

        AuthenticateAs(requester);

        var userToUpdate = await UserRepo.CreateUserAsync(
            "test@test.de",
            role: UserRole.Moderator);

        var requestDto = new UpdateUserRequestDto
        {
            FirstName = "ChangedName",
            LastName = userToUpdate.LastName,
            Role = userToUpdate.Role
        };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.AdminUserRouting}/{userToUpdate.Guid}",
            requestDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedUser = await GetUserFromDbByGuidAsync(userToUpdate.Guid);

        updatedUser.Should().NotBeNull();
        updatedUser!.FirstName.Should().Be(requestDto.FirstName);
        updatedUser.LastName.Should().Be(updatedUser.LastName);
        updatedUser.LoginAttempts.Should().Be(userToUpdate.LoginAttempts);
        updatedUser.Role.Should().Be(updatedUser.Role);
        updatedUser.Status.Should().Be(userToUpdate.Status);
        updatedUser.UpdatedByUserGuid.Should().Be(requester.Guid);
        updatedUser.UpdatedDate.Should().BeCloseTo(DateTime.UtcNow,
            precision: TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Tests Update User – Success – Just Last Name.
    /// </summary>
    [Fact]
    public async Task UpdateUser_Success_JustLastName()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test1@test.de",
            role: UserRole.Admin);

        AuthenticateAs(requester);

        var userToUpdate = await UserRepo.CreateUserAsync(
            "test@test.de",
            role: UserRole.Moderator);

        var requestDto = new UpdateUserRequestDto
        {
            FirstName = userToUpdate.FirstName,
            LastName = "ChangedLastName",
            Role = userToUpdate.Role
        };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.AdminUserRouting}/{userToUpdate.Guid}",
            requestDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedUser = await GetUserFromDbByGuidAsync(userToUpdate.Guid);

        updatedUser.Should().NotBeNull();
        updatedUser!.FirstName.Should().Be(userToUpdate.FirstName);
        updatedUser.LastName.Should().Be(requestDto.LastName);
        updatedUser.LoginAttempts.Should().Be(userToUpdate.LoginAttempts);
        updatedUser.Role.Should().Be(updatedUser.Role);
        updatedUser.Status.Should().Be(userToUpdate.Status);
        updatedUser.UpdatedByUserGuid.Should().Be(requester.Guid);
        updatedUser.UpdatedDate.Should().BeCloseTo(DateTime.UtcNow,
            precision: TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Tests Update User – Success – Just Role.
    /// </summary>
    [Fact]
    public async Task UpdateUser_Success_JustRole()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test1@test.de",
            role: UserRole.Admin);

        AuthenticateAs(requester);

        var userToUpdate = await UserRepo.CreateUserAsync(
            "test@test.de",
            role: UserRole.Moderator);

        var requestDto = new UpdateUserRequestDto
        {
            FirstName = userToUpdate.FirstName,
            LastName = userToUpdate.LastName,
            Role = UserRole.User
        };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.AdminUserRouting}/{userToUpdate.Guid}",
            requestDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedUser = await GetUserFromDbByGuidAsync(userToUpdate.Guid);

        updatedUser.Should().NotBeNull();
        updatedUser!.FirstName.Should().Be(userToUpdate.FirstName);
        updatedUser.LastName.Should().Be(userToUpdate.LastName);
        updatedUser.LoginAttempts.Should().Be(userToUpdate.LoginAttempts);
        updatedUser.Role.Should().Be(requestDto.Role);
        updatedUser.Status.Should().Be(userToUpdate.Status);
        updatedUser.UpdatedByUserGuid.Should().Be(requester.Guid);
        updatedUser.UpdatedDate.Should().BeCloseTo(DateTime.UtcNow,
            precision: TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Tests Update User – Error Code – No Changes.
    /// </summary>
    [Fact]
    public async Task UpdateUser_ErrorCode_NoChanges()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test1@test.de",
            role: UserRole.Admin);

        AuthenticateAs(requester);

        var userToUpdate = await UserRepo.CreateUserAsync(
            "test@test.de",
            role: UserRole.Moderator);

        var requestDto = new UpdateUserRequestDto
        {
            FirstName = userToUpdate.FirstName,
            LastName = userToUpdate.LastName,
            Role = userToUpdate.Role
        };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.AdminUserRouting}/{userToUpdate.Guid}",
            requestDto);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoUpdatedData);
    }

    /// <summary>
    /// Tests Update User – No Active User – Error Code.
    /// </summary>
    [Fact]
    public async Task UpdateUser_NoActiveUser_ErrorCode()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test1@test.de",
            role: UserRole.Admin);

        AuthenticateAs(requester);

        var userToUpdate = await UserRepo.CreateUserAsync(
            "test@test.de",
            status: UserStatus.Deleted,
            role: UserRole.Moderator);

        var requestDto = new UpdateUserRequestDto
        {
            FirstName = userToUpdate.FirstName,
            LastName = userToUpdate.LastName,
            Role = userToUpdate.Role
        };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.AdminUserRouting}/{userToUpdate.Guid}",
            requestDto);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoUserFound);
    }

    /// <summary>
    /// Tests Update User – No Permission – Forbidden.
    /// </summary>
    [Fact]
    public async Task UpdateUser_NoPermission_Forbidden()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test1@test.de",
            role: UserRole.Moderator);

        AuthenticateAs(requester);

        var userToUpdate = await UserRepo.CreateUserAsync(
            "test@test.de",
            role: UserRole.Moderator);

        var requestDto = new UpdateUserRequestDto
        {
            FirstName = userToUpdate.FirstName,
            LastName = userToUpdate.LastName,
            Role = userToUpdate.Role
        };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.AdminUserRouting}/{userToUpdate.Guid}",
            requestDto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    /// <summary>
    /// Tests Update User – Update Own User – Forbidden.
    /// </summary>
    [Fact]
    public async Task UpdateUser_UpdateOwnUser_Forbidden()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test1@test.de",
            role: UserRole.Moderator);

        AuthenticateAs(requester);

        var requestDto = new UpdateUserRequestDto
        {
            FirstName = "ChangedFirstName",
            LastName = requester.LastName,
            Role = requester.Role
        };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.AdminUserRouting}/{requester.Guid}",
            requestDto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    /// <summary>
    /// Tests Update User – As Moderator – Can Update User.
    /// </summary>
    [Fact]
    public async Task UpdateUser_AsModerator_CanUpdateUser()
    {
        var requester = await UserRepo.CreateUserAsync(
            "mod@test.de",
            role: UserRole.Moderator);

        AuthenticateAs(requester);

        var userToUpdate = await UserRepo.CreateUserAsync(
            "user@test.de",
            role: UserRole.User);

        var requestDto = new UpdateUserRequestDto
        {
            FirstName = "ChangedByModerator",
            LastName = userToUpdate.LastName,
            Role = userToUpdate.Role
        };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.AdminUserRouting}/{userToUpdate.Guid}",
            requestDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedUser = await GetUserFromDbByGuidAsync(userToUpdate.Guid);
        updatedUser!.FirstName.Should().Be("ChangedByModerator");
        updatedUser.UpdatedByUserGuid.Should().Be(requester.Guid);
    }

    /// <summary>
    /// Tests Update User – As Moderator – On Admin – Returns Forbidden.
    /// </summary>
    [Fact]
    public async Task UpdateUser_AsModerator_OnAdmin_ReturnsForbidden()
    {
        var requester = await UserRepo.CreateUserAsync(
            "mod@test.de",
            role: UserRole.Moderator);

        AuthenticateAs(requester);

        var userToUpdate = await UserRepo.CreateUserAsync(
            "admin@test.de",
            role: UserRole.Admin);

        var requestDto = new UpdateUserRequestDto
        {
            FirstName = "ShouldNotApply",
            LastName = userToUpdate.LastName,
            Role = userToUpdate.Role
        };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.AdminUserRouting}/{userToUpdate.Guid}",
            requestDto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    /// <summary>
    /// Tests Update User – Target Is Blocked – Returns Object Is Not Active.
    /// </summary>
    [Fact]
    public async Task UpdateUser_TargetIsBlocked_ReturnsObjectIsNotActive()
    {
        var requester = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        AuthenticateAs(requester);

        var userToUpdate = await UserRepo.CreateUserAsync(
            "blocked@test.de", status: UserStatus.Blocked, role: UserRole.User);

        var requestDto = new UpdateUserRequestDto
        {
            FirstName = "ChangedName",
            LastName = userToUpdate.LastName,
            Role = userToUpdate.Role
        };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.AdminUserRouting}/{userToUpdate.Guid}",
            requestDto);

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.ObjectIsNotActive);
    }

    #endregion

    #region ActivateUser / BlockUser

    /// <summary>
    /// Tests Block User – Success – Sets Status Blocked.
    /// </summary>
    [Fact]
    public async Task BlockUser_Success_SetsStatusBlocked()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        AuthenticateAs(admin);

        var userToBlock = await UserRepo.CreateUserAsync("user@test.de", role: UserRole.User);

        var response = await Client.PostAsync(
            $"{Routings.AdminUserRouting}/{userToBlock.Guid}/block",
            new StringContent(string.Empty));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await GetUserFromDbByGuidAsync(userToBlock.Guid);
        updated!.Status.Should().Be(UserStatus.Blocked);
        updated.UpdatedByUserGuid.Should().Be(admin.Guid);
    }

    /// <summary>
    /// Tests Block User – Self – Returns Forbidden.
    /// </summary>
    [Fact]
    public async Task BlockUser_Self_ReturnsForbidden()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        AuthenticateAs(admin);

        var response = await Client.PostAsync(
            $"{Routings.AdminUserRouting}/{admin.Guid}/block",
            new StringContent(string.Empty));

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    /// <summary>
    /// Tests Activate User – Blocked User – Sets Status Active.
    /// </summary>
    [Fact]
    public async Task ActivateUser_BlockedUser_SetsStatusActive()
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        AuthenticateAs(admin);

        var blocked = await UserRepo.CreateUserAsync(
            "blocked@test.de", status: UserStatus.Blocked, role: UserRole.User);

        var response = await Client.PostAsync(
            $"{Routings.AdminUserRouting}/{blocked.Guid}/activate",
            new StringContent(string.Empty));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await GetUserFromDbByGuidAsync(blocked.Guid);
        updated!.Status.Should().Be(UserStatus.Active);
        updated.UpdatedByUserGuid.Should().Be(admin.Guid);
    }

    /// <summary>
    /// Tests Activate User – Success.
    /// </summary>
    [Fact]
    public async Task ActivateUser_Success()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test1@test.de",
            role: UserRole.Admin);

        var deleter = await UserRepo.CreateUserAsync(
            "test2@test.de",
            role: UserRole.Admin);

        AuthenticateAs(requester);

        var userToActivate = await UserRepo.CreateUserAsync(
            "test@test.de",
            status: UserStatus.Deleted,
            deleter: deleter.Guid,
            role: UserRole.Moderator);

        var response = await Client.PostAsync(
            $"{Routings.AdminUserRouting}/{userToActivate.Guid}/activate",
            new StringContent(string.Empty));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedUser = await GetUserFromDbByGuidAsync(userToActivate.Guid);

        updatedUser.Should().NotBeNull();
        updatedUser!.Status.Should().Be(UserStatus.Active);
        updatedUser.DeletedByUserGuid.Should().Be(null);
        updatedUser.DeletionDate.Should().Be(null);
        updatedUser.UpdatedByUserGuid.Should().Be(requester.Guid);
        updatedUser.UpdatedDate.Should().BeCloseTo(DateTime.UtcNow,
            precision: TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Tests Activate User – Already Active – Returns User Already Active.
    /// </summary>
    [Fact]
    public async Task ActivateUser_AlreadyActive_ReturnsUserAlreadyActive()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test1@test.de",
            role: UserRole.Admin);

        AuthenticateAs(requester);

        var userToActivate = await UserRepo.CreateUserAsync(
            "test@test.de",
            status: UserStatus.Active,
            role: UserRole.Moderator);

        var response = await Client.PostAsync(
            $"{Routings.AdminUserRouting}/{userToActivate.Guid}/activate",
            new StringContent(string.Empty));

        var error = await ReadErrorAsync(response);

        error.ErrorCode.Should().Be((int)ErrorCode.UserAlreadyActive);
    }

    #endregion

    #region Helpers

    private static string CreateQueryFilterUrl(
        int page,
        int pageSize,
        string? sortBy = null,
        SortDirection? sortDirection = null,
        string? filterParameter = null,
        string? filter = null)
    {
        var url = QueryHelpers.AddQueryString(
            Routings.AdminUserRouting,
            new Dictionary<string, string?>
            {
                ["sortBy"] = sortBy,
                ["sortDirection"] = sortDirection.ToString(),
                ["page"] = page.ToString(),
                ["pageSize"] = pageSize.ToString()
            });

        if (filterParameter != null)
        {
            url += "&" + $"{filterParameter}={filter}";
        }

        return url;
    }

    #endregion
}
