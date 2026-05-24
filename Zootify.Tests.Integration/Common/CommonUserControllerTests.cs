using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Common;

public class CommonUserControllerTests : IntegrationTestBase
{
    public CommonUserControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    #region GetUser

    [Fact]
    public async Task GetUser_ValidGuid_ReturnsUser()
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

        var response = await Client.
            GetFromJsonAsync<GetUserResponseDto>(
                $"{Routings.CommonUserRouting}/{userToGet.Guid}");
        
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

        var response = await Client.
            GetFromJsonAsync<GetUserResponseDto>(
                $"{Routings.CommonUserRouting}/{userToGet.Guid}");
        
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

        var response = await Client.
            GetFromJsonAsync<GetUserResponseDto>(
                $"{Routings.CommonUserRouting}/{userToGet.Guid}");
        
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

        var response = await Client.
            GetFromJsonAsync<GetUserResponseDto>(
                $"{Routings.CommonUserRouting}/{userToGet.Guid}");
        
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
    
    [Fact]
    public async Task GetUser_InvalidGuid_ReturnsNoUser()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test1@test.de",
            role: UserRole.Admin);
        
        AuthenticateAs(requester);

        var response = await Client.
            GetAsync(
                $"{Routings.CommonUserRouting}/{Guid.CreateVersion7()}");

        var error = await ReadErrorAsync(response);
        
        error.Should().NotBeNull();
        error.ErrorCode.Should().Be((int)ErrorCode.NoUserWithThisGuid);
    }
    
    [Fact]
    public async Task GetUser_InvalidRequester_ReturnsForbidden()
    {
        var requester = await UserRepo.CreateUserAsync(
            "test1@test.de",
            role: UserRole.User);
        
        AuthenticateAs(requester);

        var response = await Client.
            GetAsync(
                $"{Routings.CommonUserRouting}/{Guid.CreateVersion7()}");

        var error = await ReadErrorAsync(response);
        
        error.Should().NotBeNull();
        error.ErrorCode.Should().Be((int)ErrorCode.Forbidden);
    }

    #endregion
    
    #region UpdateOwnUser
    
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
        error.ErrorCode.Should().Be((int)ErrorCode.NoUserWithThisGuid);
    }
    
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
    
    #endregion
}