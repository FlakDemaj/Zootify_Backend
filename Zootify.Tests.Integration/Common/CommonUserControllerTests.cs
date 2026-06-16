using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Common;

/// <summary>
/// Tests for Common User Controller.
/// </summary>
public class CommonUserControllerTests : IntegrationTestBase
{
    public CommonUserControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    #region GetOwnUserData

    /// <summary>
    /// Tests Get Own User – Valid User – Returns User.
    /// </summary>
    [Fact]
    public async Task GetOwnUser_ValidUser_ReturnsUser()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test1@test.de",
            role: UserRole.Admin);

        AuthenticateAs(requester);

        var response = await Client.GetFromJsonAsync<GetOwnUserDataResponse>(
            $"{Routings.CommonUserRouting}/ownData");

        response.Should().NotBeNull();

        response!.FirstName.Should().Be(requester.FirstName);
        response.LastName.Should().Be(requester.LastName);
        response.Email.Should().Be(requester.Email);
    }

    #endregion

    #region UpdateOwnUser

    /// <summary>
    /// Tests Update Own User – Success – All Data.
    /// </summary>
    [Fact]
    public async Task UpdateOwnUser_Success_AllData()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test1@test.de",
            role: UserRole.Admin);

        AuthenticateAs(requester);

        var requestDto = new UpdateOwnUserRequestDto
        {
            FirstName = "ChangedName",
            LastName = "ChangedLastName",
            Email = "changedEmail@test.de"
        };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonUserRouting}",
            requestDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedUser = await GetUserFromDbByGuidAsync(requester.Guid);

        updatedUser.Should().NotBeNull();
        updatedUser!.FirstName.Should().Be(requestDto.FirstName);
        updatedUser.LastName.Should().Be(requestDto.LastName);
        updatedUser.Email.Should().Be(requestDto.Email.ToLower());
        updatedUser.UpdatedByUserGuid.Should().Be(requester.Guid);
        updatedUser.UpdatedDate.Should().BeCloseTo(DateTime.UtcNow,
            precision: TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Tests Update Own User – Success – Just Email.
    /// </summary>
    [Fact]
    public async Task UpdateOwnUser_Success_JustEmail()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test1@test.de",
            role: UserRole.Admin);

        AuthenticateAs(requester);

        var requestDto = new UpdateOwnUserRequestDto
        {
            FirstName = requester.FirstName,
            LastName = requester.LastName,
            Email = "changedEmail@test.de"
        };

        var response = await Client.PatchAsJsonAsync(
            $"{Routings.CommonUserRouting}",
            requestDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedUser = await GetUserFromDbByGuidAsync(requester.Guid);

        updatedUser.Should().NotBeNull();
        updatedUser!.FirstName.Should().Be(requester.FirstName);
        updatedUser.LastName.Should().Be(requester.LastName);
        updatedUser.Email.Should().Be(requestDto.Email.ToLower());
        updatedUser.UpdatedByUserGuid.Should().Be(requester.Guid);
        updatedUser.UpdatedDate.Should().BeCloseTo(DateTime.UtcNow,
            precision: TimeSpan.FromSeconds(5));
    }

    #endregion

    #region DeleteUser

    /// <summary>
    /// Tests Delete User – Success.
    /// </summary>
    [Fact]
    public async Task DeleteUser_Success()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test1@test.de",
            role: UserRole.Admin);

        AuthenticateAs(requester);

        var userToDelete = await UserRepo.CreateUserAsync(
            "test@test.de",
            role: UserRole.Moderator);

        var response = await Client.DeleteAsync(
            $"{Routings.CommonUserRouting}/{userToDelete.Guid}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedUser = await GetUserFromDbByGuidAsync(userToDelete.Guid);

        updatedUser.Should().NotBeNull();
        updatedUser!.Status.Should().Be(UserStatus.Deleted);
        updatedUser.DeletedByUserGuid.Should().Be(requester.Guid);
        updatedUser.DeletionDate.Should().BeCloseTo(DateTime.UtcNow,
            precision: TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Tests Delete User – No User To Delete Found – Error Code.
    /// </summary>
    [Fact]
    public async Task DeleteUser_NoUserToDeleteFound_ErrorCode()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test1@test.de",
            role: UserRole.Admin);

        AuthenticateAs(requester);

        var userToDelete = await UserRepo.CreateUserAsync(
            "test@test.de",
            status: UserStatus.Deleted,
            role: UserRole.Moderator);

        var response = await Client.DeleteAsync(
            $"{Routings.CommonUserRouting}/{userToDelete.Guid}");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoUserFound);
    }

    /// <summary>
    /// Tests Delete User – Moderator Trys To Delete Another User – Error Code.
    /// </summary>
    [Fact]
    public async Task DeleteUser_ModeratorTrysToDeleteAnotherUser_ErrorCode()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test1@test.de",
            role: UserRole.Moderator);

        AuthenticateAs(requester);

        var userToDelete = await UserRepo.CreateUserAsync(
            "test@test.de",
            status: UserStatus.Deleted,
            role: UserRole.Moderator);

        var response = await Client.DeleteAsync(
            $"{Routings.CommonUserRouting}/{userToDelete.Guid}");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    /// <summary>
    /// Tests Delete User – User Trys To Delete Another User – Error Code.
    /// </summary>
    [Fact]
    public async Task DeleteUser_UserTrysToDeleteAnotherUser_ErrorCode()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test1@test.de",
            role: UserRole.User);

        AuthenticateAs(requester);

        var userToDelete = await UserRepo.CreateUserAsync(
            "test@test.de",
            status: UserStatus.Deleted,
            role: UserRole.Moderator);

        var response = await Client.DeleteAsync(
            $"{Routings.CommonUserRouting}/{userToDelete.Guid}");

        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    /// <summary>
    /// Tests Delete User – User Deletes Himself – Success.
    /// </summary>
    [Fact]
    public async Task DeleteUser_UserDeletesHimself_Success()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test1@test.de",
            role: UserRole.User);

        AuthenticateAs(requester);

        var response = await Client.DeleteAsync(
            $"{Routings.CommonUserRouting}/{requester.Guid}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedUser = await GetUserFromDbByGuidAsync(requester.Guid);

        updatedUser.Should().NotBeNull();
        updatedUser!.DeletedByUserGuid.Should().Be(requester.Guid);
        updatedUser.DeletionDate.Should().BeCloseTo(DateTime.UtcNow,
            precision: TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Tests Delete User – Moderator Deletes User – Success.
    /// </summary>
    [Fact]
    public async Task DeleteUser_ModeratorDeletesUser_Success()
    {
        var moderator = await UserRepo.CreateUserAsync("mod@test.de", role: UserRole.Moderator);
        AuthenticateAs(moderator);

        var userToDelete = await UserRepo.CreateUserAsync("user@test.de", role: UserRole.User);

        var response = await Client.DeleteAsync(
            $"{Routings.CommonUserRouting}/{userToDelete.Guid}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await GetUserFromDbByGuidAsync(userToDelete.Guid);
        updated!.Status.Should().Be(UserStatus.Deleted);
        updated.DeletedByUserGuid.Should().Be(moderator.Guid);
    }

    #endregion

}