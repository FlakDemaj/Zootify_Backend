using Zootify_Backend.DTOs.Admin.Request;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Admin;

public class UserControllerTests : IntegrationTestBase
{
    public UserControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    #region UpdateUser

    
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
        
        var response = await Client.PostAsJsonAsync(
            $"{Routings.AdminUserRouting}/update/{userToUpdate.Guid}",
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
        
        var response = await Client.PostAsJsonAsync(
            $"{Routings.AdminUserRouting}/update/{userToUpdate.Guid}",
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
        
        var response = await Client.PostAsJsonAsync(
            $"{Routings.AdminUserRouting}/update/{userToUpdate.Guid}",
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
        
        var response = await Client.PostAsJsonAsync(
            $"{Routings.AdminUserRouting}/update/{userToUpdate.Guid}",
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
    
    [Fact]
    public async Task UpdateUser_Success_NoChanges()
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
        
        var response = await Client.PostAsJsonAsync(
            $"{Routings.AdminUserRouting}/update/{userToUpdate.Guid}",
            requestDto);
        
        var error = await ReadErrorAsync(response);
        error.ErrorCode.Should().Be((int)ErrorCode.NoUpdatedData);
    }
    
    [Fact]
    public async Task UpdateUser_Success_Forbidden()
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
        
        var response = await Client.PostAsJsonAsync(
            $"{Routings.AdminUserRouting}/update/{userToUpdate.Guid}",
            requestDto);
        
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion
}