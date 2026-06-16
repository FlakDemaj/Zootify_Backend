# syntax=docker/dockerfile:1

# ---- Build-Stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Zuerst nur das Projektfile kopieren und restoren -> bessere Layer-Caches.
COPY Zootify_Backend/Zootify_Backend.csproj Zootify_Backend/
RUN dotnet restore Zootify_Backend/Zootify_Backend.csproj

# Restlichen Quellcode kopieren und veröffentlichen.
COPY Zootify_Backend/ Zootify_Backend/
RUN dotnet publish Zootify_Backend/Zootify_Backend.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime-Stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish ./
# config.json (Default-Werte) wird über eine relative Quelle geladen und ist nicht Teil
# der Standard-Publish-Ausgabe -> explizit in den Content-Root kopieren.
COPY --from=build /src/Zootify_Backend/config.json ./config.json

# Das aspnet-Image lauscht standardmäßig auf Port 8080.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Zootify_Backend.dll"]
