using Microsoft.EntityFrameworkCore;

namespace Zootify.Tests.Integration.Common;

[Collection("Integration")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly IntegrationTestFixture Fixture;
    protected readonly HttpClient Client;
    protected readonly UserTestRepository _userRepo;

    private readonly ZootifyDbContext _dbContext;

    protected IntegrationTestBase(IntegrationTestFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.Client;
        _dbContext = fixture.DbContext;
        _userRepo = new UserTestRepository(_dbContext);
    }

    public virtual async Task InitializeAsync()
    {
        Client.DefaultRequestHeaders.Clear();
        _dbContext.ChangeTracker.Clear();
        await Fixture.ResetAsync();
    }

    public virtual Task DisposeAsync() => Task.CompletedTask;

    protected async Task<UserEntity?> GetUserFromDbAsync(string email)
    {
        return await _dbContext.Set<UserEntity>()
            .AsNoTracking()
            .Include(u => u.RegistrationToken)
            .Include(u => u.ResetPasswordToken)
            .FirstOrDefaultAsync(u => u.Email == email.ToLower());
    }

    protected static async Task<ErrorDto> ReadErrorAsync(HttpResponseMessage response)
    {
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        error.Should().NotBeNull();
        return error!;
    }
}
