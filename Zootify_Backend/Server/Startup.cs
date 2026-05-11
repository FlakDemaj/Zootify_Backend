using System.Security.Claims;
using System.Text;
using Autofac;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Logging.Console;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Models.DataAccess.DBContext;
using Zootify_Backend.Persistance;
using Zootify_Backend.Server.Middlewares;
using AuthenticationMiddleware = Zootify_Backend.Server.Middlewares.AuthenticationMiddleware;

namespace Zootify_Backend.Server;

/// <summary>
/// Configures the application's DI container, services, and HTTP middleware pipeline.
/// </summary>
public class Startup
{
    private static readonly Type[] Modules =
    [
        typeof(ZootifyModule)
    ];
    private static readonly string[] OptionsAction = ["de", "en"];

    /// <summary>
    /// Registers Autofac modules including the main application module and the AutoMapper module.
    /// </summary>
    public void ConfigureContainer(ContainerBuilder builder)
    {
        builder.RegisterModule<ZootifyModule>();

        builder.RegisterModule(
            new AutoMapperModule
            {
                Types = Modules
            });
    }

    /// <summary>
    /// Configures all application services (Dependency Injection container).
    /// </summary>
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        EnableCors(builder.Services);

        ConfigureLogger(builder);
        builder.Services.AddControllers();

        AddSwagger(builder.Services);
        AddAuthentication(builder.Services, builder.Configuration);
        AddInternalServices(builder.Services);
        AddDatabase(builder.Services);

    }

    /// <summary>
    /// Configures the HTTP request pipeline (middleware order).
    /// </summary>
    public void ConfigureMiddleware(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseCors("AllowOrigin");

        app.UseRequestLocalization(options =>
        {
            var supported = OptionsAction;
            options.SetDefaultCulture("de")
                   .AddSupportedCultures(supported)
                   .AddSupportedUICultures(supported);

            // Use only the Accept-Language header; remove other providers.
            options.RequestCultureProviders =
            [
                new AcceptLanguageHeaderRequestCultureProvider()
            ];
        });

        app.UseAuthentication();
        app.UseAuthorization();

        EnableMiddlewares(app);
        app.MapControllers();
    }

    /// <summary>
    /// Registers the <see cref="ZootifyDbContext"/> and the <see cref="MigrationManager"/> with the DI container.
    /// </summary>
    private void AddDatabase(IServiceCollection services)
    {
        services.AddScoped<MigrationManager>();
        services.AddDbContext<ZootifyDbContext>();
    }

    /// <summary>
    /// Adds Swagger configuration including JWT authentication support.
    /// </summary>
    private void AddSwagger(IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using Bearer scheme",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(document =>
            {
                var securityRequirement = new OpenApiSecurityRequirement();
                var schemeReference = new OpenApiSecuritySchemeReference("Bearer", document);
                securityRequirement.Add(schemeReference, []);
                return securityRequirement;
            });
        });
    }

    /// <summary>
    /// Configures JWT authentication (issuer, audience, signing key).
    /// </summary>
    private void AddAuthentication(
        IServiceCollection services,
        ConfigurationManager configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                RoleClaimType = ClaimTypes.Role,
                ValidIssuer = configuration["AccessToken:Issuer"],
                ValidAudience = configuration["AccessToken:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(configuration["AccessToken:Key"]!))
            });
    }

    /// <summary>
    /// Configures logging providers and log formatting.
    /// </summary>
    private void ConfigureLogger(WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
            options.IncludeScopes = true;
            options.UseUtcTimestamp = true;
            options.ColorBehavior = builder.Environment.IsProduction()
                ? LoggerColorBehavior.Disabled
                : LoggerColorBehavior.Enabled;
        });

        builder.Logging.SetMinimumLevel(LogLevel.Trace);
    }

    /// <summary>
    /// Adds CORS policy configuration.
    /// </summary>
    private void EnableCors(IServiceCollection service)
    {
        service.AddCors(option => option.AddPolicy("AllowOrigin",
            policy => policy.AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader()));
    }

    /// <summary>
    /// Registers custom middleware components.
    /// </summary>
    private void EnableMiddlewares(IApplicationBuilder builder)
    {
        builder.UseMiddleware<ExceptionMiddleware>();
        builder.UseMiddleware<AuthenticationMiddleware>();
    }

    /// <summary>
    /// Registers internal singleton services such as <see cref="ITokenManager"/>.
    /// </summary>
    private static void AddInternalServices(IServiceCollection services)
    {
        services.AddScoped<ITokenManager, TokenManager>();
    }
}
