using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Zootify_Backend.Model.DataAccess.DBContext;
using Zootify_Backend.Model.Providers.Nominatim;
using Zootify_Backend.Model.Providers.SignalR;
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
    protected readonly CollectionTestRepository CollectionRepo;
    protected readonly LoanTestRepository LoanRepo;
    protected Mock<INominatimProvider> NominatimMock => Fixture.NominatimMock;
    protected Mock<IHubContext<NotificationHub>> HubContextMock => Fixture.HubContextMock;

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
        CollectionRepo = new CollectionTestRepository(_dbContext);
        LoanRepo = new LoanTestRepository(_dbContext);
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

    protected async Task<CollectionEntity?> GetCollectionFromDbAsync(Guid guid)
    {
        return await _dbContext.Set<CollectionEntity>()
            .AsNoTracking()
            .Include(c => c.Collection)
            .FirstOrDefaultAsync(c => c.Guid == guid);
    }

    protected async Task<SpecimenCollectionEntity?> GetSpecimenCollectionFromDbAsync(
        Guid collectionGuid,
        Guid specimenGuid)
    {
        return await _dbContext.Set<SpecimenCollectionEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(sc => sc.CollectionGuid == collectionGuid && sc.SpecimenGuid == specimenGuid);
    }

    protected async Task<UserSettingsEntity?> GetUserSettingsFromDbAsync(Guid userGuid)
    {
        return await _dbContext.Set<UserSettingsEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserGuid == userGuid);
    }

    protected async Task CreateUserSettingsInDbAsync(Guid userGuid,
        bool darkMode = false,
        bool sendEmail = true,
        bool sendPush = true,
        string language = "de")
    {
        // Users created via UserTestRepository already have a default settings row, so update it when
        // present (one settings row per user is enforced by a unique constraint) and insert otherwise.
        var settings = await _dbContext.Set<UserSettingsEntity>()
            .FirstOrDefaultAsync(s => s.UserGuid == userGuid);

        if (settings == null)
        {
            settings = new UserSettingsEntity { UserGuid = userGuid };
            _dbContext.Add(settings);
        }

        settings.DarkMode = darkMode;
        settings.Language = language;
        settings.SendEmail = sendEmail;
        settings.SendPushNotification = sendPush;

        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();
    }

    protected async Task<LoanEntity?> GetLoanFromDbAsync(Guid guid)
    {
        return await _dbContext.Set<LoanEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Guid == guid);
    }

    protected async Task<TaxonomyNodeEntity?> GetTaxonomyNodeFromDbAsync(Guid guid)
    {
        return await _dbContext.Set<TaxonomyNodeEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Guid == guid);
    }

    /// <summary>
    /// Creates a new DI scope backed by the test application's service provider.
    /// Use this to resolve scoped services (e.g. jobs, purge services) directly in tests.
    /// </summary>
    protected IServiceScope CreateScope() => Fixture.Services.CreateScope();

    /// <summary>
    /// Overrides the deletion_date column for any soft-deletable entity via EF Core's
    /// change-tracker API, which bypasses the private setter on DeletionDate.
    /// </summary>
    protected async Task OverrideDeletionDateAsync(object entity, DateTime deletionDate)
    {
        _dbContext.ChangeTracker.Clear();
        _dbContext.Attach(entity);
        _dbContext.Entry(entity).Property("DeletionDate").CurrentValue = deletionDate;
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();
    }

    protected void AuthenticateAs(UserEntity user)
    {
        var token = GenerateTestToken(user.Role, user.Guid);
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Sets the <c>Accept-Language</c> header for subsequent requests, which the localization
    /// middleware translates into <see cref="System.Globalization.CultureInfo.CurrentUICulture"/>.
    /// Headers are cleared before each test, so this does not leak between tests.
    /// </summary>
    protected void SetAcceptLanguage(string language)
    {
        Client.DefaultRequestHeaders.Remove("Accept-Language");
        Client.DefaultRequestHeaders.Add("Accept-Language", language);
    }

    /// <summary>
    /// Verifies that an in-app push notification whose message contains the given text was sent the
    /// expected number of times via SignalR.
    /// </summary>
    /// <param name="messageContains">A substring expected in the notification message (e.g. a language-specific word).</param>
    /// <param name="times">The expected number of matching sends.</param>
    protected void VerifyPushNotificationSent(string messageContains, Times times) =>
        Fixture.HubClientProxyMock.Verify(p => p.SendCoreAsync(
            It.IsAny<string>(),
            It.Is<object[]>(args => args.Length >= 1 && ((string)args[0]).Contains(messageContains)),
            It.IsAny<CancellationToken>()), times);

    /// <summary>
    /// Verifies that no in-app push notification was sent via SignalR.
    /// </summary>
    protected void VerifyNoPushNotificationSent() =>
        Fixture.HubClientProxyMock.Verify(p => p.SendCoreAsync(
            It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Never);
    
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
