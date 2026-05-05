using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Data;
using Zootify_Backend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // Swagger UI für das Testen der API

// Datenbankverbindung registrieren
builder.Services.AddDbContext<ZootifyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Geocoding-Service registrieren — UserAgent ist Pflicht laut Nominatim-Nutzungsbedingungen
builder.Services.AddHttpClient<IGeocodingService, NominatimGeocodingService>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Zootify/1.0");
});

var app = builder.Build();

// Swagger nur im Entwicklungsmodus aktivieren
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers(); // Alle Controller-Routen registrieren

app.Run();
