using System.Security.Claims;
using System.Text;
using Autofac;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging.Console;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Zootify_Backend.Models.DataAccess.DBContext;
using Zootify_Backend.Persistance;
using Zootify_Backend.Server.Middlewares;

namespace Zootify_Backend.Server;

public class Startup
{
    
    // List of modules to be registered with AutoMapper
    private static readonly Type[] Modules =
    [
        typeof(ZootifyModule)
    ];
    
    // Configures Autofac container and registers necessary modules
    public void ConfigureContainer(ContainerBuilder builder)
    {
        // Registers the main application module
        builder.RegisterModule<ZootifyModule>();
        
        // Registers the AutoMapper module with specified types
        builder.RegisterModule(
            new AutoMapperModule
            {
                Types = Modules
            });
    }
    
    /// <summary>
    /// Configures all application services (Dependency Injection container).
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder instance</param>
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        EnableCors(builder.Services);

        ConfigureLogger(builder);
        builder.Services.AddControllers();

        AddSwagger(builder.Services);
       //AddAuthentication(builder.Services, builder.Configuration);
        
        AddDatabase(builder.Services);
    }
    
    /// <summary>
    /// Configures the HTTP request pipeline (middleware order).
    /// </summary>
    /// <param name="app">The WebApplication instance</param>
    public void ConfigureMiddleware(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseCors("AllowOrigin");
        app.UseAuthentication();
        app.UseAuthorization();

        EnableMiddlewares(app);
        app.MapControllers();
    }

    private void AddDatabase(IServiceCollection services)
    {
        services.AddScoped<MigrationManager>();
        services.AddDbContext<ZootifyDbContext>();
    }


    /// <summary>
    /// Adds Swagger configuration including JWT authentication support.
    /// </summary>
    /// <param name="services">The service collection</param>
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
    /// <param name="services">The service collection</param>
    /// <param name="configuration">Application configuration</param>
   /* private void AddAuthentication(
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
    }*/

    /// <summary>
    /// Configures logging providers and log formatting.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder instance</param>
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
    /// <param name="service">The service collection</param>
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
    /// <param name="builder">The application builder</param>
    private void EnableMiddlewares(IApplicationBuilder builder)
    {
        builder.UseMiddleware<ExceptionMiddleware>();
    }
}