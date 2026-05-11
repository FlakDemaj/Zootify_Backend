using Autofac;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Zootify_Backend.Server;

namespace Zootify.Tests.Integration.Common;

/// <summary>
/// Replaces ZootifyDbContext with the test container DB and IBrevoProvider with a mock.
/// </summary>
public class ZootifyTestFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly Mock<IBrevoProvider> _brevoMock;

    /// <summary>
    /// Initializes the factory with the test container connection string and a Brevo mock.
    /// </summary>
    public ZootifyTestFactory(string connectionString, Mock<IBrevoProvider> brevoMock)
    {
        _connectionString = connectionString;
        _brevoMock = brevoMock;
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
                ["AccessToken:Key"] = "ZootifyTestSecretKeyThatIsAtLeast32BytesLong!",
                ["AccessToken:Issuer"] = "Zootify-Server",
                ["AccessToken:Audience"] = "Zootify-Client",
                ["AccessToken:ExpiresInMinutes"] = "30"
            });
        });

        builder.ConfigureServices(services =>
        {
            var existingOptions = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ZootifyDbContext>));
            if (existingOptions != null)
                services.Remove(existingOptions);

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
        });

        return base.CreateHost(builder);
    }
}
