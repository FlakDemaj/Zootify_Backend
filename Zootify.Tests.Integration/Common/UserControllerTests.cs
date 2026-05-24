using Zootify_Backend.DTOs.Common.Response;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Common;

public class UserControllerTests : IntegrationTestBase
{
    public UserControllerTests(IntegrationTestFixture fixture) : base(fixture)
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
}