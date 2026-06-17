using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Zootify_Backend.Server.Swagger;

/// <summary>
/// Adds an optional <c>Accept-Language</c> header parameter to every operation, so the request culture
/// (e.g. "de" or "en") can be selected directly in the Swagger UI. The chosen value is honored by the
/// <see cref="Microsoft.AspNetCore.Localization.AcceptLanguageHeaderRequestCultureProvider"/> configured
/// in the request pipeline.
/// </summary>
public class AcceptLanguageHeaderOperationFilter : IOperationFilter
{
    // Must mirror the supported cultures configured for request localization in Startup.
    private static readonly string[] SupportedCultures = ["de", "en"];

    /// <summary>
    /// Appends the <c>Accept-Language</c> header parameter to the given operation.
    /// </summary>
    /// <param name="operation">The operation being documented.</param>
    /// <param name="context">The current operation filter context.</param>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= [];

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "Accept-Language",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Sprache/Culture der Antwort (z. B. Fehlermeldungen). Standard: de.",
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Default = "de",
                Enum = SupportedCultures.Select(c => (JsonNode)c).ToList()
            }
        });
    }
}
