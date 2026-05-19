using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Zootify.Tests.Integration.Utils.Helpers;

namespace Zootify.Tests.Integration.Utils;

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
    
    protected void AuthenticateAs(UserEntity user)
    {
        var token = GenerateTestToken(user.Role, user.Guid);
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }
    
    private static string GenerateTestToken(UserRole role, Guid userGuid)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userGuid.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
            new(ClaimTypes.Role, role.ToString()),
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(TestConfig.TestJwtSecret));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: TestConfig.TestIssuer,
            audience: TestConfig.TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
