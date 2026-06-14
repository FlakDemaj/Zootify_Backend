using Autofac;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Module = Autofac.Module;

namespace Zootify_Backend;

/// <summary>
/// Autofac module for configuring AutoMapper.
/// This module registers a singleton instance of the mapper.
/// </summary>
public class AutoMapperModule : Module
{
    /// <summary>
    /// Types to be scanned by AutoMapper to load mapping profiles.
    /// </summary>
    public Type[] Types { get; init; } = null!;

    /// <summary>
    /// Configures AutoMapper and registers it in the DI container.
    /// </summary>
    /// <param name="builder">The Autofac container builder.</param>
    protected override void Load(ContainerBuilder builder)
    {
        var expression = new MapperConfigurationExpression();
        expression.AllowNullCollections = true;
        expression.AddMaps(Types);

        var defaultConfig = new MapperConfiguration(expression, NullLoggerFactory.Instance);
        defaultConfig.AssertConfigurationIsValid();

        builder.RegisterInstance(defaultConfig.CreateMapper())
            .As<IMapper>()
            .SingleInstance();
    }
}