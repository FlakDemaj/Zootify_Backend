using Autofac;
using SendWithBrevo;
using Zootify_Backend.Configuration;
using Zootify_Backend.Extensions;
using Zootify_Backend.Models.Providers;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Server;

public class ZootifyModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Registers TransactionInterceptor with a per-lifetime scope
        builder.RegisterType<TransactionInterceptor>()
            .InstancePerLifetimeScope();
        
        builder.LoadServicesAndRepositories(ThisAssembly);
        
        LoadEmailProvider(builder);
    }
    
    // Configures email provider service registration
    private void LoadEmailProvider(ContainerBuilder builder)
    {
        // Registers an instance of BrevoClient as a singleton using a configured API key
        builder.RegisterInstance(new BrevoClient(ZootifyConfiguration.BrevoApiKey))
            .SingleInstance();

        // Registers BrevoProvider as its interface IBrevoProvider with a per-lifetime scope
        builder.RegisterType<BrevoProvider>()
            .As<IBrevoProvider>()
            .InstancePerLifetimeScope();
    }
}