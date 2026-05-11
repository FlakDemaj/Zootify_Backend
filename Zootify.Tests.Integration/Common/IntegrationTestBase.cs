using Microsoft.EntityFrameworkCore;

namespace Zootify.Tests.Integration.Common;

/// <summary>
/// Base class for all integration tests. Provides a pre-configured HTTP client,
/// database context, and helper utilities. Resets shared state before each test.
/// </summary>
[Collection("Integration")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly IntegrationTestFixture Fixture;
    protected readonly HttpClient Client;
    protected readonly UserTestRepository UserRepo;

    private readonly ZootifyDbContext _dbContext;

    /// <summary>
    /// Initializes the test base by resolving the shared HTTP client, database context,
    /// and user repository from the fixture.
    /// </summary>
    protected IntegrationTestBase(IntegrationTestFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.Client;
        _dbContext = fixture.DbContext;
        UserRepo = new UserTestRepository(_dbContext);
    }

    /// <summary>
    /// Clears HTTP headers, the EF change tracker, and resets the database to a clean state
    /// before each test runs.
    /// </summary>
    public virtual async Task InitializeAsync()
    {
        Client.DefaultRequestHeaders.Clear();
        _dbContext.ChangeTracker.Clear();
        await Fixture.ResetAsync();
    }

    /// <summary>
    /// No-op dispose — shared resources are managed by <see cref="IntegrationTestFixture"/>.
    /// </summary>
    public virtual Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Fetches a user from the database by email, eagerly loading their registration and reset-password tokens.
    /// Returns <c>null</c> if no user with the given email exists.
    /// </summary>
    protected async Task<UserEntity?> GetUserFromDbByEmailAsync(string email)
    {
        return await _dbContext.Set<UserEntity>()
            .AsNoTracking()
            .Include(u => u.RegistrationToken)
            .Include(u => u.ResetPasswordToken)
            .FirstOrDefaultAsync(u => u.Email == email.ToLower());
    }

    /// <summary>
    /// Deserializes the error response body into an <see cref="ErrorDto"/> and asserts it is not null.
    /// </summary>
    protected static async Task<ErrorDto> ReadErrorAsync(HttpResponseMessage response)
    {
        var error = await response.Content.ReadFromJsonAsync<ErrorDto>();
        error.Should().NotBeNull();
        return error!;
    }
}
