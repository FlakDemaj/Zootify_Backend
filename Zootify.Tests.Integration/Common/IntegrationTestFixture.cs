using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

using Respawn;

using Testcontainers.MsSql;

namespace Zootify.Tests.Integration.Common;

public class IntegrationTestFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _mssql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private ZootifyTestFactory _factory = null!;
    private Respawner _respawner = null!;
    private IServiceScope _dbScope = null!;

    public HttpClient Client { get; private set; } = null!;
    public string ConnectionString { get; private set; } = null!;
    public Mock<IBrevoProvider> BrevoMock { get; } = new();
    public IServiceProvider Services => _factory.Services;
    public ZootifyDbContext DbContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _mssql.StartAsync();
        ConnectionString = _mssql.GetConnectionString();

        _factory = new ZootifyTestFactory(ConnectionString, BrevoMock);
        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        _dbScope = _factory.Services.CreateScope();
        DbContext = _dbScope.ServiceProvider.GetRequiredService<ZootifyDbContext>();

        await DbContext.Database.EnsureCreatedAsync();
        await InitializeRespawnerAsync();
    }

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
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        _dbScope.Dispose();
        await _factory.DisposeAsync();
        await _mssql.DisposeAsync();
    }
}
