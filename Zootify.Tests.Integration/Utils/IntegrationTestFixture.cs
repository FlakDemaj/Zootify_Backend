using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Respawn;
using Testcontainers.MsSql;
using Zootify_Backend.Model.Providers;
using Zootify_Backend.Model.Providers.Brevo;
using Zootify_Backend.Model.Providers.Nominatim;

namespace Zootify.Tests.Integration.Utils;

/// <summary>
/// Shared fixture for the integration test collection.
/// Starts a SQL Server Testcontainer, creates the schema, and provides
/// a resettable database and a Brevo mock for each test.
/// </summary>
public class IntegrationTestFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _mssql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private ZootifyTestFactory _factory = null!;
    private Respawner _respawner = null!;
    private IServiceScope _dbScope = null!;

    public HttpClient Client { get; private set; } = null!;
    private string ConnectionString { get; set; } = null!;
    public Mock<IBrevoProvider> BrevoMock { get; } = new();
    public Mock<INominatimProvider> NominatimMock { get; } = new();
    public ZootifyDbContext DbContext { get; private set; } = null!;

    /// <summary>
    /// Starts the SQL Server container, creates the HTTP client and database context,
    /// and configures Respawn for fast table truncation between tests.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _mssql.StartAsync();
        ConnectionString = _mssql.GetConnectionString();

        _factory = new ZootifyTestFactory(ConnectionString, BrevoMock, NominatimMock);
        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        _dbScope = _factory.Services.CreateScope();
        DbContext = _dbScope.ServiceProvider.GetRequiredService<ZootifyDbContext>();

        await DbContext.Database.EnsureCreatedAsync();
        await InitializeRespawnerAsync();
    }

    /// <summary>
    /// Opens a fresh SQL connection and configures a <see cref="Respawner"/> scoped to the
    /// application's schema so only application tables are truncated on reset.
    /// </summary>
    private async Task InitializeRespawnerAsync()
    {
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();
        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            SchemasToInclude = ["zootify"]
        });
    }

    /// <summary>Truncates all tables and resets the Brevo mock. Called before each test.</summary>
    public async Task ResetAsync()
    {
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();
        await _respawner.ResetAsync(conn);
        BrevoMock.Reset();
        NominatimMock.Reset();
    }

    /// <summary>
    /// Disposes the HTTP client, DI scope, test factory, and SQL Server container.
    /// </summary>
    public async Task DisposeAsync()
    {
        Client.Dispose();
        _dbScope.Dispose();
        await _factory.DisposeAsync();
        await _mssql.DisposeAsync();
    }
}
