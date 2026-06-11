using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zootify_Backend.Model.Services.Admin.Interfaces;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Jobs;

public class UserPurgeServiceTests : IntegrationTestBase
{
    // Default RetentionYears = 3; cutoff = now - 3 years
    private static readonly DateTime OldEnough   = DateTime.UtcNow.AddYears(-4); // beyond retention
    private static readonly DateTime TooRecent   = DateTime.UtcNow.AddYears(-1); // within retention

    public UserPurgeServiceTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task PurgeBatch_DeletedUserBeyondRetention_IsHardDeleted()
    {
        var user = await UserRepo.CreateUserAsync("old@test.de", status: UserStatus.Deleted);
        await OverrideDeletionDateAsync(user, OldEnough);

        using var scope = CreateScope();
        var purged = await scope.ServiceProvider.GetRequiredService<IUserPurgeService>().PurgeBatchAsync();

        purged.Should().Be(1);
        var inDb = await Fixture.DbContext.Set<UserEntity>().AsNoTracking()
            .FirstOrDefaultAsync(u => u.Guid == user.Guid);
        inDb.Should().BeNull();
    }

    [Fact]
    public async Task PurgeBatch_DeletedUserWithinRetention_IsNotPurged()
    {
        var user = await UserRepo.CreateUserAsync("recent@test.de", status: UserStatus.Deleted);
        await OverrideDeletionDateAsync(user, TooRecent);

        using var scope = CreateScope();
        var purged = await scope.ServiceProvider.GetRequiredService<IUserPurgeService>().PurgeBatchAsync();

        purged.Should().Be(0);
        var inDb = await Fixture.DbContext.Set<UserEntity>().AsNoTracking()
            .FirstOrDefaultAsync(u => u.Guid == user.Guid);
        inDb.Should().NotBeNull();
    }

    [Fact]
    public async Task PurgeBatch_ActiveUser_IsNotPurged()
    {
        var user = await UserRepo.CreateUserAsync("active@test.de", status: UserStatus.Active);

        using var scope = CreateScope();
        var purged = await scope.ServiceProvider.GetRequiredService<IUserPurgeService>().PurgeBatchAsync();

        purged.Should().Be(0);
        var inDb = await Fixture.DbContext.Set<UserEntity>().AsNoTracking()
            .FirstOrDefaultAsync(u => u.Guid == user.Guid);
        inDb.Should().NotBeNull();
    }

    [Fact]
    public async Task PurgeBatch_NoEligibleUsers_ReturnsZero()
    {
        using var scope = CreateScope();
        var purged = await scope.ServiceProvider.GetRequiredService<IUserPurgeService>().PurgeBatchAsync();

        purged.Should().Be(0);
    }

    [Fact]
    public async Task PurgeBatch_MultipleEligible_AllPurged()
    {
        var u1 = await UserRepo.CreateUserAsync("old1@test.de", status: UserStatus.Deleted);
        var u2 = await UserRepo.CreateUserAsync("old2@test.de", status: UserStatus.Deleted);
        var u3 = await UserRepo.CreateUserAsync("active@test.de", status: UserStatus.Active);
        await OverrideDeletionDateAsync(u1, OldEnough);
        await OverrideDeletionDateAsync(u2, OldEnough);

        using var scope = CreateScope();
        var purged = await scope.ServiceProvider.GetRequiredService<IUserPurgeService>().PurgeBatchAsync();

        purged.Should().Be(2);
        var u3InDb = await Fixture.DbContext.Set<UserEntity>().AsNoTracking()
            .FirstOrDefaultAsync(u => u.Guid == u3.Guid);
        u3InDb.Should().NotBeNull();
    }
}
