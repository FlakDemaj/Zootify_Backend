using System.Reflection;
using Autofac;
using Autofac.Builder;
using Autofac.Extras.DynamicProxy;
using Autofac.Features.Scanning;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Extensions;

/// <summary>
/// Extensions for the ContainerBuilder
/// </summary>
public static class ContainerBuilderExtensions
{
    /// <summary>
    /// Registers all the Managers and Repositorys
    /// </summary>
    /// <param name="builder">The container builder.</param>
    /// <param name="assembly">The assembly to scan for types.</param>
    public static void LoadServicesAndRepositories(
        this ContainerBuilder builder,
        Assembly assembly)
    {
        builder.RegisterAssemblyTypesEndsWith(assembly, "Service", "Repository");
    }

    /// <summary>
    /// Registers assembly types ending with "Mapper" with custom default configuration.
    /// </summary>
    /// <param name="builder">The container builder.</param>
    /// <param name="assembly">The assembly to scan for types.</param>
    public static void LoadMapper(
        this ContainerBuilder builder,
        Assembly assembly)
    {
        builder.RegisterAssemblyTypes(assembly)
            .Where(
                type => type.Name.EndsWith("Mapper"))
            .DefineWithDefaultConfigurationForMapper()
            .PreserveExistingDefaults();
    }

    /// <summary>
    /// Defines the default configuration for registered types, enabling lifetime scope, interface interception, and transaction interception.
    /// </summary>
    /// <param name="registration">The registration builder.</param>
    /// <returns>The configured registration builder.</returns>
    private static IRegistrationBuilder<object, ScanningActivatorData, DynamicRegistrationStyle>
        DefineWithDefaultConfiguration(
            this IRegistrationBuilder<object, ScanningActivatorData, DynamicRegistrationStyle>
                registration) =>
        registration.InstancePerLifetimeScope()
            .AsImplementedInterfaces()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(TransactionInterceptor));

    /// <summary>
    /// Defines the default configuration for mapper types, setting instance lifetime scope without additional interceptors.
    /// </summary>
    /// <param name="registration">The registration builder.</param>
    /// <returns>The configured registration builder.</returns>
    private static IRegistrationBuilder<object, ScanningActivatorData, DynamicRegistrationStyle>
        DefineWithDefaultConfigurationForMapper(
            this IRegistrationBuilder<object, ScanningActivatorData, DynamicRegistrationStyle>
                registration) =>
        registration.InstancePerLifetimeScope();

    /// <summary>
    /// Registers assembly types whose names end with specified suffixes with default configuration.
    /// </summary>
    /// <param name="builder">The container builder.</param>
    /// <param name="assembly">The assembly to scan for types.</param>
    /// <param name="endsWith">Array of suffixes to filter types.</param>
    private static void RegisterAssemblyTypesEndsWith(
        this ContainerBuilder builder,
        Assembly assembly,
        params string[] endsWith)
    {
        foreach (var ending in endsWith)
        {
            builder.RegisterAssemblyTypes(assembly)
                   .Where(
                       type => type.Name.EndsWith(ending))
                   .DefineWithDefaultConfiguration()
                   .PreserveExistingDefaults();
        }
    }
}
