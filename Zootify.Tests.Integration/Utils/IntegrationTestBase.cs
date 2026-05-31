using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Zootify_Backend.Model.Providers.Nominatim;
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
    protected readonly SpecimenTestRepository SpecimenRepo;
    protected readonly TagTestRepository TagRepo;
    protected Mock<INominatimProvider> NominatimMock => Fixture.NominatimMock;

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
        SpecimenRepo = new SpecimenTestRepository(_dbContext);
        TagRepo = new TagTestRepository(_dbContext);
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
    /// Fetches a user from the database by email, eagerly loading their registration and reset-password tokens.
    /// Returns <c>null</c> if no user with the given email exists.
    /// </summary>
    protected async Task<UserEntity?> GetUserFromDbByGuidAsync(Guid guid)
    {
        return await _dbContext.Set<UserEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Guid == guid);
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
    
    protected async Task<SpecimenEntity?> GetSpecimenFromDbAsync(Guid guid)
    {
        return await _dbContext.Set<SpecimenEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Guid == guid);
    }

    protected async Task<ApprovalSpecimenEntity?> GetApprovalForSpecimenFromDbAsync(Guid specimenGuid)
    {
        return await _dbContext.Set<ApprovalSpecimenEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.SpecimenGuid == specimenGuid);
    }

    protected async Task<List<GeographicLocationEntity>> GetAllGeographicLocationsFromDbAsync()
    {
        return await _dbContext.Set<GeographicLocationEntity>()
            .AsNoTracking()
            .ToListAsync();
    }

    protected async Task<List<RelationSpecimensTagEntity>> GetTagRelationsForSpecimenFromDbAsync(Guid specimenGuid)
    {
        return await _dbContext.Set<RelationSpecimensTagEntity>()
            .AsNoTracking()
            .Where(r => r.SpecimenGuid == specimenGuid)
            .ToListAsync();
    }

    protected async Task<List<SpecimenImageEntity>> GetImagesForSpecimenFromDbAsync(Guid specimenGuid)
    {
        return await _dbContext.Set<SpecimenImageEntity>()
            .AsNoTracking()
            .Where(i => i.SpecimenGuid == specimenGuid)
            .ToListAsync();
    }

    protected async Task<TaxonomyNodeEntity?> GetTaxonomyNodeFromDbAsync(Guid guid)
    {
        return await _dbContext.Set<TaxonomyNodeEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Guid == guid);
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
