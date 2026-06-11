using Autofac;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Zootify_Backend.Model.DataAccess.DBContext;
using Zootify_Backend.Model.Providers;
using Zootify_Backend.Model.Providers.Brevo;
using Zootify_Backend.Model.Providers.Nominatim;
using Zootify_Backend.Model.Providers.SignalR;
using Zootify_Backend.Server;
using Zootify.Tests.Integration.Utils.Helpers;

namespace Zootify.Tests.Integration.Utils;

/// <summary>
/// Replaces ZootifyDbContext with the test container DB and IBrevoProvider with a mock.
/// </summary>
public class ZootifyTestFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly Mock<IBrevoProvider> _brevoMock;
    private readonly Mock<INominatimProvider> _nominatimMock;
    private readonly Mock<IHubContext<NotificationHub>> _hubContextMock;

    /// <summary>
    /// Initializes the factory with the test container connection string and provider mocks.
    /// </summary>
    public ZootifyTestFactory(
        string connectionString,
        Mock<IBrevoProvider> brevoMock,
        Mock<INominatimProvider> nominatimMock,
        Mock<IHubContext<NotificationHub>> hubContextMock)
    {
        _connectionString = connectionString;
        _brevoMock = brevoMock;
        _nominatimMock = nominatimMock;
        _hubContextMock = hubContextMock;
    }
    

    /// <summary>
    /// Overrides the application configuration and replaces the database context
    /// with one pointed at the test container.
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTest");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AccessToken:Key"] = TestConfig.TestJwtSecret,
                ["AccessToken:Issuer"] = TestConfig.TestIssuer,
                ["AccessToken:Audience"] = TestConfig.TestAudience,
                ["AccessToken:ExpiresInMinutes"] = TestConfig.TestTokenExpiresInMinutes.ToString(),
                ["Brevo:ApiKey"] = TestConfig.TestBrevoApiKey
            });
        });

        builder.ConfigureServices(services =>
        {
            var existingOptions = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ZootifyDbContext>));
            if (existingOptions != null)
            {
                services.Remove(existingOptions);
            }

            services.AddDbContext<ZootifyDbContext>(options =>
                options.UseSqlServer(_connectionString));
        });
    }

    /// <summary>
    /// Runs last so the mock registration overrides the real BrevoProvider from Program.cs.
    /// </summary>
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureContainer<ContainerBuilder>(autofac =>
        {
            autofac.RegisterInstance(_brevoMock.Object)
                .As<IBrevoProvider>()
                .SingleInstance();

            autofac.RegisterInstance(_nominatimMock.Object)
                .As<INominatimProvider>()
                .SingleInstance();

            autofac.RegisterInstance(_hubContextMock.Object)
                .As<IHubContext<NotificationHub>>()
                .SingleInstance();
        });

        return base.CreateHost(builder);
    }
}
