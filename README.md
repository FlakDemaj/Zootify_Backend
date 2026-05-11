# Zootify Backend

Backend-API fuer die Plattform zur Dokumentation von Zoologischen Sammlungen.  
Entwickelt mit **ASP.NET Core**, **Entity Framework Core** (SQL Server), **Autofac** und **Evolve** fuer Migrationen.

---

## Voraussetzungen

| Tool | Zweck |
|------|-------|
| [.NET 10 SDK](https://dotnet.microsoft.com/download) | Runtime und Build |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | SQL Server lokal starten **und** Integration-Tests (Testcontainers) |
| [SQL Server](https://www.microsoft.com/de-de/sql-server/) | Alternativ zu Docker (z.B. lokale Express-Installation) |

---

## Schnellstart mit Docker (empfohlen)

### 1. SQL Server starten

```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 --name zootify-sql \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

Oder als `docker-compose.yml` im Projektroot:

```yaml
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      SA_PASSWORD: "YourStrong@Passw0rd"
    ports:
      - "1433:1433"
    volumes:
      - zootify-data:/var/opt/mssql

volumes:
  zootify-data:
```

```bash
docker compose up -d
```

### 2. User Secrets setzen

```bash
cd Zootify_Backend

dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=localhost,1433;Database=ZootifyDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"

dotnet user-secrets set "AccessToken:Key" "DeinGeheimesJwtSchluesselMindestens32Zeichen!"

dotnet user-secrets set "Brevo:ApiKey" "xkeysib-dein-brevo-api-key"
```

### 3. App starten

```bash
cd Zootify_Backend
dotnet run
```

Die API ist erreichbar unter:
- HTTP:  `http://localhost:5138`
- HTTPS: `https://localhost:7227`
- Swagger: `http://localhost:5138/swagger`

---

## User Secrets (Pflichtfelder)

Die App liest Konfiguration in dieser Reihenfolge (spaetere Quellen ueberschreiben fruehere):

1. `config.json` (im Projektordner, eingecheckt — keine Secrets dort!)
2. User Secrets (nur im `Development`-Environment)
3. Umgebungsvariablen

**UserSecretsId:** `cfebb19e-0299-4285-babe-8bb6ee0ed4cd`  
Gespeichert unter: `~/.microsoft/usersecrets/cfebb19e-.../secrets.json`

### Komplette `secrets.json` (Beispiel)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=ZootifyDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"
  },
  "AccessToken": {
    "Key": "DeinGeheimesJwtSchluesselMindestens32Zeichen!"
  },
  "Brevo": {
    "ApiKey": "xkeysib-dein-brevo-api-key"
  }
}
```

### Alle Schluessel im Ueberblick

| Schluessel | Pflicht | Beschreibung |
|------------|---------|--------------|
| `ConnectionStrings:DefaultConnection` | Ja | SQL Server Connection String |
| `ConnectionStrings:OverrideConnectionString` | Nein | Fallback wenn `DefaultConnection` leer |
| `AccessToken:Key` | Ja | JWT Signing Key (mind. 32 Zeichen!) |
| `AccessToken:Issuer` | Nein | Standard: `Zootify-Server` (in config.json) |
| `AccessToken:Audience` | Nein | Standard: `Zootify-Client` (in config.json) |
| `AccessToken:ExpiresInMinutes` | Nein | Standard: `30` (in config.json) |
| `Brevo:ApiKey` | Ja | API-Key von [brevo.com](https://app.brevo.com) fuer E-Mail-Versand |
| `Brevo:Email` | Nein | Standard: `zootify@gmx.de` (in config.json) |
| `Brevo:SenderName` | Nein | Standard: `Zootify` (in config.json) |

### Connection String Formate

```
# Docker / SQL Server mit SA-Login:
Server=localhost,1433;Database=ZootifyDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True

# Windows Auth (lokale Instanz):
Server=localhost;Database=ZootifyDb;Trusted_Connection=True;TrustServerCertificate=True

# SQL Server Express:
Server=.\SQLEXPRESS;Database=ZootifyDb;Trusted_Connection=True;TrustServerCertificate=True
```

---

## Migrationen

Migrationen laufen **automatisch beim App-Start** ueber [Evolve](https://evolve-db.netlify.app/).  
SQL-Skripte liegen unter `Persistance/Migrations/` und folgen dem Namensschema `V{Jahr}_{Nr}__{Beschreibung}.sql`.

Neue Migration hinzufuegen:

```bash
# Datei erstellen (Beispiel):
touch Zootify_Backend/Persistance/Migrations/V2026_2__Add_NewTable.sql
# SQL-Inhalt einfuegen, beim naechsten dotnet run wird sie automatisch ausgefuehrt
```

> **Hinweis:** Im `Testing`- und `IntegrationTest`-Environment werden Migrationen **nicht** automatisch ausgefuehrt (Testcontainers uebernimmt das selbst).

---

## Tests

### Unit-Tests

Kein Docker erforderlich.

```bash
cd Zootify_Backend2
dotnet test Zootify.Tests.Unit
```

### Integration-Tests

**Docker muss laufen!** Die Tests starten automatisch einen SQL Server via [Testcontainers](https://dotnet.testcontainers.org/).  
Brevo wird durch einen Mock ersetzt — kein echter API-Key noetig.

```bash
dotnet test Zootify.Tests.Integration
```

Alle Tests auf einmal:

```bash
dotnet test
```

---

## Projektstruktur

```
Zootify_Backend2/
├── Zootify_Backend/                  # Haupt-API-Projekt
│   ├── config.json                   # Basis-Konfiguration (keine Secrets!)
│   ├── Controllers/
│   │   ├── Auth/                     # Login, Registrierung, Passwort-Reset
│   │   ├── Base/                     # Basis-Controller-Klassen
│   │   └── System/                   # Health-/System-Endpunkte
│   ├── DTOs/                         # Request/Response-Objekte
│   ├── Model/
│   │   ├── Auth/                     # TokenManager (JWT-Erstellung)
│   │   ├── DataAccess/
│   │   │   ├── DBContext/            # ZootifyDbContext (EF Core)
│   │   │   ├── Entities/             # Datenbank-Entitaeten
│   │   │   └── EntityConfigurations/ # Fluent API Konfigurationen
│   │   ├── EmailTemplates/           # HTML-E-Mail-Vorlagen (embedded)
│   │   ├── Mappers/                  # AutoMapper Profile
│   │   ├── Providers/                # BrevoProvider (E-Mail-Versand)
│   │   └── Services/                 # Business-Logik
│   ├── Persistance/
│   │   ├── MigrationManager.cs       # Evolve-Integration
│   │   └── Migrations/               # SQL-Migrationsskripte (V{Jahr}_{Nr}__{Name}.sql)
│   ├── Properties/
│   │   └── launchSettings.json       # Ports: HTTP 5138, HTTPS 7227
│   ├── Server/
│   │   ├── Middlewares/              # ExceptionMiddleware
│   │   ├── Program.cs                # Einstiegspunkt
│   │   ├── Startup.cs                # Services, Middleware-Pipeline
│   │   └── ZootifyModule.cs          # Autofac DI-Registrierungen
│   └── Utils/
│       ├── Configuration/            # ZootifyConfiguration (statischer Config-Accessor)
│       ├── Enums/                    # Anwendungsweite Enumerationen
│       ├── Extensions/               # Extension Methods
│       ├── Transaction/              # Transaction-Helper
│       └── ZootifyException.cs       # Eigene Exception-Klasse
│
├── Zootify.Tests.Unit/               # Unit-Tests (kein Docker)
│   └── Domain/                       # Domain-Logik-Tests
│
├── Zootify.Tests.Integration/        # Integration-Tests (Docker erforderlich)
│   ├── config.json                   # Test-Konfiguration (kein echter API-Key)
│   ├── Auth/                         # Auth-Controller-Tests
│   └── Common/
│       ├── ZootifyTestFactory.cs     # WebApplicationFactory mit Testcontainers-DB
│       ├── IntegrationTestFixture.cs # Container-Lifecycle und Respawn
│       └── Helpers/                  # Test-Helper und Factories
│
└── Zootify.Tests.Shared/             # Gemeinsame Test-Utilities
```

---

## Datenmodell

### Tabellen-Uebersicht

Die Datenbank besteht aus 6 Tabellen:

---

#### Users (Benutzer)

| Spalte | Typ | Beschreibung |
|--------|-----|-------------|
| Id | int | Primaerschluessel, wird automatisch vergeben |
| FirstName | string (max 100) | Vorname |
| LastName | string (max 100) | Nachname |
| Email | string (max 256) | E-Mail-Adresse, muss einzigartig sein (fuer Login) |
| PasswordHash | string (max 512) | Gehashtes Passwort (BCrypt) |
| Role | string | Rolle: `User`, `Moderator` oder `Admin` |
| Status | string | Kontostatus: `Pending`, `Active` oder `Blocked` |
| CreatedAt | datetime | Zeitpunkt der Registrierung |

---

#### TaxonomyNodes (SCOFGA-Hierarchie)

Bildet die biologische Klassifikation als Baumstruktur ab (Stamm bis Art, Self-Reference).

| Spalte | Typ | Beschreibung |
|--------|-----|-------------|
| Id | int | Primaerschluessel |
| Name | string (max 256) | Name des Taxons |
| Rank | string | `Phylum`, `Class`, `Order`, `Family`, `Genus`, `Species` |
| ApprovalStatus | string | `Pending`, `Approved`, `Rejected` |
| ParentId | int? | Uebergeordneter Knoten (null = Wurzel) |
| ProposedByUserId | int? | Vorschlagender Benutzer |

---

#### GeographicLocations (Geographische Hierarchie)

| Spalte | Typ | Beschreibung |
|--------|-----|-------------|
| Id | int | Primaerschluessel |
| Name | string (max 256) | Name des Ortes |
| Level | string | `Continent`, `Country`, `State`, `District`, `City` |
| ParentId | int? | Uebergeordneter Eintrag (null = Kontinent) |

---

#### Specimens (Funde / Individuen)

Das **Kernobjekt** der Sammlung.

| Spalte | Typ | Beschreibung |
|--------|-----|-------------|
| Id | int | Primaerschluessel |
| SpeciesId | int | Art (TaxonomyNode mit Rank=Species) |
| GeographicLocationId | int? | Fundort-Zuordnung |
| Latitude | double? | Breitengrad |
| Longitude | double? | Laengengrad |
| FindDate | datetime? | Funddatum |
| OwnerId | int | Besitzer (User) |
| StorageBuilding / Cabinet / Shelf / Compartment | string? | Aufbewahrungsort |
| Comment | string? (max 2000) | Freitext |
| IsPublic | bool | Oeffentlich sichtbar |
| CreatedAt / UpdatedAt | datetime | Timestamps |

---

#### SpecimenImages (Bilder)

| Spalte | Typ | Beschreibung |
|--------|-----|-------------|
| Id | int | Primaerschluessel |
| SpecimenId | int | Zugehoeriger Fund |
| ImageUrl | string (max 1000) | Pfad oder URL |
| UploadedAt | datetime | Upload-Zeitpunkt |

---

#### Loans (Ausleihen)

| Spalte | Typ | Beschreibung |
|--------|-----|-------------|
| Id | int | Primaerschluessel |
| SpecimenId | int | Ausgeliehenes Objekt |
| LenderId | int | Verleiher (User) |
| BorrowerId | int? | Ausleihender Nutzer (optional) |
| BorrowerExternalName | string? (max 256) | Externer Ausleiher (kein Account) |
| LoanDate | datetime | Verleih-Datum |
| DueDate | datetime | Rueckgabe-Deadline |
| ReturnDate | datetime? | Tatsaechliche Rueckgabe (null = noch ausgeliehen) |
| Notes | string? (max 2000) | Anmerkungen |

---

### Loeschverhalten

| Beziehung | Verhalten | Grund |
|-----------|-----------|-------|
| TaxonomyNode -> Parent | **Restrict** | Kein Loeschen wenn Kinder vorhanden |
| GeographicLocation -> Parent | **Restrict** | Kein Loeschen wenn Unterorte vorhanden |
| Specimen -> Species | **Restrict** | Art nicht loeschbar wenn in Gebrauch |
| Specimen -> Owner (User) | **Cascade** | User geloescht = Funde werden mitgeloescht |
| Specimen -> GeographicLocation | **SetNull** | Ort geloescht = Referenz wird null |
| SpecimenImage -> Specimen | **Cascade** | Fund geloescht = Bilder werden mitgeloescht |
| Loan -> Specimen | **Cascade** | Fund geloescht = Ausleihen werden mitgeloescht |
| Loan -> Lender/Borrower | **Restrict** | Kein User loeschen mit aktiven Ausleihen |

---

## Technologie-Stack

| Bereich | Technologie |
|---------|-------------|
| Runtime | .NET 10 |
| Framework | ASP.NET Core (REST-API) |
| ORM | Entity Framework Core 10 |
| Datenbank | Microsoft SQL Server |
| Migrationen | Evolve (SQL-Skript-basiert) |
| DI-Container | Autofac |
| Mapping | AutoMapper |
| Auth | JWT Bearer (Microsoft.AspNetCore.Authentication.JwtBearer) |
| Passwort-Hashing | BCrypt.Net-Next |
| E-Mail | Brevo (SendWithBrevo) |
| API-Dokumentation | Swagger / OpenAPI |
| Unit-Tests | xUnit + Moq + FluentAssertions |
| Integration-Tests | xUnit + Testcontainers (SQL Server) + Respawn |
