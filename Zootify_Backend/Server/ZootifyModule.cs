using Autofac;
using SendWithBrevo;
using Zootify_Backend.Configuration;
using Zootify_Backend.Extensions;
using Zootify_Backend.Model.Providers;
using Zootify_Backend.Model.Providers.Brevo;
using Zootify_Backend.Model.Providers.Nominatim;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Server;

/// <summary>
/// Autofac module that wires up all application-level services, repositories, and providers.
/// </summary>
public class ZootifyModule : Module
{
    /// <summary>
    /// Registers the transaction interceptor, all services and repositories, and the email provider.
    /// </summary>
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<TransactionInterceptor>()
            .InstancePerLifetimeScope();

        builder.LoadServicesAndRepositories(ThisAssembly);

        LoadEmailProvider(builder);
    }

    /// <summary>
    /// Registers the <see cref="BrevoClient"/> singleton and the <see cref="BrevoProvider"/>
    /// scoped to a per-lifetime-scope instance.
    /// </summary>
    private void LoadEmailProvider(ContainerBuilder builder)
    {
        builder.RegisterInstance(new BrevoClient(ZootifyConfiguration.BrevoApiKey))
            .SingleInstance();

        builder.RegisterType<BrevoProvider>()
            .As<IBrevoProvider>()
            .InstancePerLifetimeScope();
        
        builder.RegisterType<NominatimProvider>()
            .As<INominatimProvider>()
            .InstancePerLifetimeScope();
    }
}
