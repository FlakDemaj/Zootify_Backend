# Zootify Backend

Backend-API für die Plattform zur Dokumentation zoologischer Sammlungen (Funde/Individuen, Taxonomie,
Sammlungen, Ausleihen, Bilder). Entwickelt mit **ASP.NET Core (.NET 10)**, **Entity Framework Core 10**
(SQL Server), **Autofac** und **Evolve** für die Datenbank-Migrationen.

Die Lösung besteht aus vier Projekten:

| Projekt | Zweck |
|---------|-------|
| `Zootify_Backend` | Die eigentliche REST-API (Server) |
| `Zootify.Tests.Unit` | Unit-Tests (ohne Docker) |
| `Zootify.Tests.Integration` | Integrationstests (Docker erforderlich, Testcontainers) |
| `Zootify.Tests.Shared` | Gemeinsame Test-Bausteine (Builder, Testdaten) |

---

## Inhaltsverzeichnis

- [Voraussetzungen](#voraussetzungen)
- [IDE-Empfehlung (Rider)](#ide-empfehlung-rider)
- [Schnellstart](#schnellstart)
- [Konfiguration](#konfiguration)
- [Migrationen](#migrationen)
- [Projektstruktur (Server)](#projektstruktur-server)
- [Architektur-Überblick](#architektur-überblick)
- [API](#api)
- [Datenmodell](#datenmodell)
- [Hintergrund-Jobs](#hintergrund-jobs)
- [Tests](#tests)
- [Technologie-Stack](#technologie-stack)
- [Lizenz](#lizenz)

---

## Voraussetzungen

| Tool | Zweck |
|------|-------|
| [.NET 10 SDK](https://dotnet.microsoft.com/download) | Build und Runtime |
| Zugang zur **Uni-Datenbank** | Wird als SQL-Server-Datenbank verwendet (Connection-String via `.env`) |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | API-Container (optional) **und** Integrationstests (Testcontainers) |

> **Hinweis:** Es wird die Datenbank der Uni genutzt – es muss also **kein** eigener SQL Server betrieben
> werden. Die gesamte Konfiguration (inkl. Verbindungsdaten) kommt aus einer `.env`-Datei im Repo-Root
> (nicht eingecheckt). Falls die Uni-DB nur über VPN erreichbar ist, muss das VPN aktiv sein.
>
> Das **.NET-SDK** wird für lokale Entwicklung (`dotnet run`) und die Tests (`dotnet test`) benötigt. Für den
> reinen Betrieb der API per Docker genügt **Docker** – der Build läuft dann im Container (siehe `Dockerfile`).

---

## IDE-Empfehlung (Rider)

Am besten (aber **nicht** zwingend) wird das Projekt mit [JetBrains Rider](https://www.jetbrains.com/rider/)
geöffnet – einfach die Solution-Datei `Zootify_Backend.sln` öffnen.

Im **Solution-Explorer** finden Sie neben den Projekten den Ordner **`SolutionItems`**. Dort liegen die
projektübergreifenden Dateien gebündelt und direkt bearbeitbar:

- `Dockerfile`, `docker-compose.yml`, `.dockerignore`
- `.env.example`
- `.gitignore`
- `README.md`
- `LICENSE`

> Alternativ funktionieren auch Visual Studio oder VS Code – dann wird der `SolutionItems`-Ordner ggf.
> anders bzw. gar nicht angezeigt; die Dateien liegen physisch im Repo-Root.

---

## Schnellstart

In **beiden** Varianten kommt die gesamte Konfiguration aus einer `.env`-Datei im Repo-Root und es wird die
**Uni-DB** verwendet. Schritt 1 (`.env` anlegen) ist daher immer nötig; danach läuft die API entweder im
Container (Variante A) oder lokal (Variante B) – beide lesen dieselbe `.env`.

### Schritt 1: `.env` anlegen (immer)

Die `.env` gehört in den **Repo-Root** – also direkt neben `docker-compose.yml` und `Zootify_Backend.sln`,
**nicht** in den Projektordner `Zootify_Backend/` (die Source):

```text
Zootify_Backend3/            ← Repo-Root  → .env HIER
├── .env
├── .env.example
├── docker-compose.yml
├── Zootify_Backend.sln
└── Zootify_Backend/         ← Source     → NICHT hier
```

```bash
# Im Repo-Root:
cp .env.example .env
```

Anschließend die Werte in `.env` eintragen:

```bash
# Connection-String zur Uni-Datenbank (vom Lehrstuhl bereitgestellt):
ConnectionStrings__DefaultConnectionString=Server=<uni-host>;Database=<db>;User Id=<user>;Password=<passwort>;TrustServerCertificate=True

# JWT-Signing-Key (mind. 32 Zeichen):
AccessToken__Key=EinGeheimerJwtSchluesselMitMindestens32Zeichen!

# Brevo-Key (nur für echten E-Mail-Versand nötig):
Brevo__ApiKey=xkeysib-ihr-brevo-api-key
```

> Die doppelten Unterstriche (`__`) entsprechen verschachtelten Konfigurationsschlüsseln
> (`ConnectionStrings__DefaultConnectionString` → `ConnectionStrings:DefaultConnectionString`). Die `.env`
> ist per `.gitignore` ausgeschlossen und wird **nicht** eingecheckt.

### Variante A: API im Docker-Container

Compose reicht die `.env` per `env_file` als Umgebungsvariablen in den Container. Es wird **kein** eigener
SQL Server gestartet – die API verbindet sich mit der Uni-DB.

```bash
# Aus dem Repo-Root:
docker compose up --build
```

Stoppen mit `docker compose down`.

> **Voraussetzung:** Schritt 1 wurde ausgeführt – ohne `.env` bricht `docker compose up` mit
> „env file ... not found" ab.

### Variante B: API lokal

Beim Start lädt die App die `.env` aus dem Repo-Root automatisch (kein `dotnet user-secrets` nötig).

```bash
# Aus dem Repo-Root:
dotnet run --project Zootify_Backend
```

> Der JWT-Key muss mindestens 32 Zeichen lang sein. Der Brevo-Key wird nur für den echten E-Mail-Versand
> benötigt – ohne ihn startet die App, der Mail-Versand schlägt dann aber fehl.

### Erreichbarkeit

| | Variante A (Docker) | Variante B (lokal) |
|--|---------------------|--------------------|
| HTTP | `http://localhost:5138` | `http://localhost:5138` |
| HTTPS | – | `https://localhost:7227` |
| Swagger UI | `http://localhost:5138/swagger` | `http://localhost:5138/swagger` |

Beim Start führt Evolve automatisch alle ausstehenden Migrationen gegen die Uni-Datenbank aus.

---

## Konfiguration

Die Konfiguration wird in dieser Reihenfolge geladen (spätere Quellen überschreiben frühere):

1. `config.json` (im Projektordner, eingecheckt – **keine Secrets**)
2. `appsettings.json`
3. **Umgebungsvariablen** – inkl. der Werte aus der `.env`-Datei

Die `.env` wird in **beiden** Betriebsarten als Umgebungsvariablen wirksam: in Docker über `env_file`
(Compose), lokal über einen kleinen Loader (`DotEnv.Load()` in `Program.cs`), der beim Start eine `.env`
aus dem Repo-Root einliest und die Werte als Umgebungsvariablen setzt (bereits gesetzte Variablen werden
nicht überschrieben). `dotnet user-secrets` wird **nicht** mehr verwendet.

Zugriff im Code erfolgt typsicher über `ZootifyConfiguration` (gestützt auf `BaseConfiguration` mit
Default-Werten und Warnung bei fehlenden Schlüsseln).

### Wichtige Schlüssel

| Schlüssel | Pflicht | Beschreibung |
|-----------|---------|--------------|
| `ConnectionStrings:DefaultConnectionString` | Ja | SQL-Server-Connection-String |
| `ConnectionStrings:OverrideConnectionString` | Nein | Wird bevorzugt, wenn gesetzt (z. B. für Migrationen) |
| `AccessToken:Key` | Ja | JWT-Signing-Key (mind. 32 Zeichen) |
| `AccessToken:Issuer` | Nein | Standard `Zootify-Server` |
| `AccessToken:Audience` | Nein | Standard `Zootify-Client` |
| `AccessToken:ExpiresInMinutes` | Nein | Standard `30` |
| `Brevo:ApiKey` | Ja* | API-Key von [brevo.com](https://app.brevo.com) für den E-Mail-Versand |
| `Brevo:Email` / `Brevo:SenderName` | Nein | Absender-Adresse / -Name |
| `General:MinimumPasswordLength` | Nein | Standard `8` |
| `General:MaximumLoginAttempts` | Nein | Standard `5` (danach wird das Konto blockiert) |
| `General:FrontendBaseUrl` | Nein | Basis-URL des Frontends (für Links in E-Mails) |
| `General:SpecialCharacters` | Nein | Erlaubte Sonderzeichen für die Passwort-Validierung |
| `General:RegistrationTokenExpiryInHours` / `ResetPasswordTokenExpiryInHours` | Nein | Token-Gültigkeit (Standard `24`) |
| `CleanupJobs:*`, `LoanReminderJob:*`, `RejectedSpecimenPurgeJob:*` | Nein | Steuerung der Hintergrund-Jobs (siehe unten) |

\* nur für tatsächlichen E-Mail-Versand erforderlich.

### Connection-String-Formate

```text
# SQL Server mit SQL-Login (z. B. die Uni-DB):
Server=<host>,1433;Database=<db>;User Id=<user>;Password=<passwort>;TrustServerCertificate=True

# Windows Auth (lokale Instanz):
Server=localhost;Database=ZootifyDb;Trusted_Connection=True;TrustServerCertificate=True

# SQL Server Express:
Server=.\SQLEXPRESS;Database=ZootifyDb;Trusted_Connection=True;TrustServerCertificate=True
```

---

## Migrationen

Die Datenbank wird über [Evolve](https://evolve-db.netlify.app/) per SQL-Skript verwaltet. Die Skripte
liegen unter `Zootify_Backend/Persistance/Migrations/` und folgen dem Namensschema
`V{Jahr}_{Monat}_{Nr}__{Beschreibung}.sql`.

- Migrationen laufen **automatisch beim App-Start** (`MigrationManager`).
- Im `Testing`-/Integrationstest-Environment werden sie **nicht** automatisch ausgeführt – dort baut die
  Testinfrastruktur das Schema selbst über `DbContext.Database.EnsureCreated()` auf.
- Alle Tabellen liegen im Schema **`zootify`**.

Neue Migration hinzufügen:

```bash
# Neue Datei nach Namensschema anlegen, z. B.:
touch Zootify_Backend/Persistance/Migrations/V2026_07_01__Add_NewTable.sql
# SQL einfügen – beim nächsten Start wird sie automatisch ausgeführt.
```

---

## Projektstruktur (Server)

```text
Zootify_Backend/
├── config.json                         # Basis-Konfiguration (keine Secrets)
├── Controllers/
│   ├── Base/BaseRestController.cs       # Basis-Controller, Route "rest/[controller]/"
│   ├── AdminAndModerator/               # Admin-/Moderator-Endpunkte (Loans, User, Taxonomie)
│   ├── Common/                          # Fachliche Endpunkte (Specimen, Collection, Tag, Taxonomy, User …)
│   ├── Home/                            # Öffentliche Übersichts-Endpunkte
│   └── System/AuthController.cs         # Login, Registrierung, Passwort-Reset
├── DTOs/                                # Request-/Response-Objekte (Base, Admin, Common, System)
├── Enums/ (Utils/Enums)                # Anwendungsweite Enumerationen + ErrorCodes
├── Model/
│   ├── Auth/                            # PasswordHasher (BCrypt), TokenManager, TokenResult
│   ├── DataAccess/
│   │   ├── DBContext/                   # ZootifyDbContext
│   │   ├── Entitys/                     # EF-Core-Entitäten (+ Base: Guid/Creation/Update/Deletion)
│   │   ├── EntityConfigurations/        # Fluent-API-Konfigurationen
│   │   └── Repositorys/                 # Repository-Schicht
│   ├── Jobs/                            # Hintergrund-Jobs (BackgroundService)
│   ├── Mappers/                         # AutoMapper-Profile
│   ├── Providers/                       # Nominatim (Geo), SignalR (Notifications), Excel, Brevo (E-Mail)
│   └── Services/                        # Business-Logik (Admin / Common / System)
├── Persistance/
│   ├── MigrationManager.cs              # Evolve-Integration
│   └── Migrations/                      # SQL-Migrationsskripte
├── Properties/launchSettings.json       # Ports: HTTP 5138, HTTPS 7227
├── Server/
│   ├── Program.cs                       # Einstiegspunkt
│   ├── Startup.cs                       # Service-Registrierung, Middleware-Pipeline
│   ├── ZootifyModule.cs                 # Autofac-DI-Registrierungen
│   └── Middlewares/                     # u. a. zentrales Exception-Handling
└── Utils/
    ├── Configuration/                   # ZootifyConfiguration / BaseConfiguration
    ├── Transaction/                     # TransactionInterceptor, IPostCommitQueue
    ├── DotEnv.cs                        # Lädt beim Start eine .env aus dem Repo-Root
    ├── FuzzyMatch.cs, ParallelNotifications.cs, HelperMethods.cs
    └── ZootifyException.cs              # Anwendungs-Exception mit ErrorCode
```

---

## Architektur-Überblick

- **Schichten:** Controller → Service (Business-Logik) → Repository → `ZootifyDbContext` (EF Core).
- **Dependency Injection:** Autofac (`ZootifyModule`).
- **Transaktionen:** Ein `TransactionInterceptor` (Castle DynamicProxy) umschließt Service-Methoden, die mit
  `[Transaction]` markiert sind. Read-only-Methoden werden zurückgerollt, schreibende Methoden committen und
  leeren anschließend die `IPostCommitQueue` – darüber werden E-Mails/Push-Notifications erst **nach** dem
  erfolgreichen Commit verschickt.
- **Soft-Delete:** Entitäten auf Basis von `BaseDeletionEntity` werden nur als gelöscht markiert; die
  Hintergrund-Jobs entfernen sie nach Ablauf der Aufbewahrungsfrist endgültig.
- **Audit:** `BaseCreationEntity` / `BaseUpdateEntity` / `BaseDeletionEntity` halten created/updated/deleted
  (Datum + verursachender User).

---

## API

- **Routen-Konvention:** `rest/[controller]/...` (definiert in `BaseRestController`).
- **Authentifizierung:** JWT Bearer. Rollen: `User`, `Moderator`, `Admin` (`[Authorize(Roles = ...)]`).
- **Dokumentation:** Swagger/OpenAPI unter `/swagger` (im Development-Environment).
- **Fehlerformat:** Fehler werden zentral über eine Middleware in eine einheitliche Antwort mit `ErrorCode`
  übersetzt; die Texte sind lokalisiert (`Resources/ErrorMessages.resx` / `.en.resx`).
- **Echtzeit:** SignalR-Hub (`NotificationHub`) für Push-Benachrichtigungen an angemeldete Clients.

---

## Datenmodell

Alle Primärschlüssel sind **`Guid`** (Spalte `guid`). Tabellen liegen im Schema `zootify`. Audit-Spalten
(`created_at`/`created_by_user_guid`, optional `updated_*`, `deleted_*`) stammen aus den Basis-Entitäten.

### Tabellen

| Tabelle | Inhalt |
|---------|--------|
| `users` | Benutzer (Name, Email **unique**, Passwort-Hash, `role`, `status`, `login_attempts`) |
| `user_settings` | Einstellungen pro User (Dark Mode, Push-/E-Mail-Benachrichtigungen, Sprache) |
| `taxonomy_nodes` | Biologische Klassifikation als Baum (`rank` Phylum→Species, `parent_guid`, `approval_status`) |
| `geo_locations` | Geografische Hierarchie (`level`, `parent_guid`) |
| `specimens` | **Kernobjekt**: Fund/Individuum (Art, Ort, Koordinaten, Lagerort, `is_public`, `status`) |
| `specimen_images` | Bilder zu Funden (Base64, `nvarchar(max)`) |
| `tags` | Frei vergebbare Tags |
| `specimens_tags` | n:m-Verknüpfung Specimen ↔ Tag |
| `collections` | Benutzer-Sammlungen (Name, Beschreibung, `private`, `status`) |
| `specimen_collection` | n:m-Verknüpfung Specimen ↔ Collection |
| `loans` | Ausleihen (Verleiher, Ausleiher, Fristen, Rückgabe, Reminder-Zeitstempel) |
| `registration_tokens` | Tokens für die Registrierungsbestätigung |
| `reset_password_tokens` | Tokens für das Zurücksetzen des Passworts |

### Wichtige Enumerationen

| Enum | Werte |
|------|-------|
| `UserRole` | `User`, `Moderator`, `Admin` |
| `UserStatus` | `Pending`, `Active`, `Blocked`, `Deleted` |
| `SpecimenStatus` | `Active`, `Pending`, `Rejected`, `Deleted` |
| `CollectionStatus` | `Active`, `Deleted` |
| `ApprovalStatus` | `Pending`, `Approved`, `Rejected` |
| `TaxonomyRank` | `Phylum`, `Class`, `Order`, `Family`, `Genus`, `Species` |

### Löschverhalten (Foreign Keys)

| Beziehung | Verhalten | Grund |
|-----------|-----------|-------|
| `taxonomy_nodes` → Parent | **Restrict** | Kein Löschen, solange Kinder existieren |
| `geo_locations` → Parent | **Restrict** | Kein Löschen, solange Unterorte existieren |
| `specimens` → Species (`species_guid`) | **SetNull** | Art entfernt → Referenz wird `null` |
| `specimens` → GeoLocation | **Restrict** | Ort bleibt referenzierbar |
| `specimen_images` → Specimen | **Cascade** | Fund gelöscht → Bilder mit |
| `specimens_tags` → Specimen / Tag | **Cascade** | Verknüpfungen werden mitgelöscht |
| `specimen_collection` → Specimen / Collection | **Cascade** | Verknüpfungen werden mitgelöscht |
| `loans` → Specimen | **Cascade** | Fund gelöscht → Ausleihen mit |
| `loans` → Lender / Borrower | **Restrict** | Kein User-Löschen mit aktiven Ausleihen |
| `user_settings` / `*_tokens` → User | **Cascade** | gehören direkt zum User |
| Audit-FKs (created/updated/deleted/approved_by) | **NoAction** | Historie bleibt erhalten; der Purge-Job anonymisiert vor dem endgültigen Löschen |

---

## Hintergrund-Jobs

Implementiert als `BackgroundService`; laufen beim Start einmalig und danach in festen Intervallen.
Jeder Batch läuft in einem eigenen DI-Scope (eigener `DbContext`).

| Job | Aufgabe | Konfiguration |
|-----|---------|---------------|
| `SpecimenPurgeJob` | Endgültiges Löschen soft-gelöschter Specimens | `CleanupJobs:*` |
| `CollectionPurgeJob` | Endgültiges Löschen soft-gelöschter Collections | `CleanupJobs:*` |
| `UserPurgeJob` | Anonymisieren der Referenzen + endgültiges Löschen von Usern | `CleanupJobs:*` |
| `RejectedSpecimenPurgeJob` | Löschen abgelehnter Specimens nach Aufbewahrungsfrist | `RejectedSpecimenPurgeJob:*` |
| `LoanReminderJob` | Mehrstufige Erinnerungen für überfällige Ausleihen | `LoanReminderJob:*` |

---

## Tests

Die Tests sind in drei Projekte aufgeteilt. Befehle vom Repo-Root ausführen.

### Unit-Tests (`Zootify.Tests.Unit`)

Kein Docker nötig. Testen Domänenlogik (Entitäten), DTOs, Mapper, Localization, Provider und Utilities
(`FuzzyMatch`). Framework: **xUnit + Moq + FluentAssertions**.

```bash
dotnet test Zootify.Tests.Unit
```

Struktur (Auszug):

```text
Zootify.Tests.Unit/
├── Domain/         # Entitäten (User, Specimen, Collection, Tokens, BaseEntitys …)
├── Dtos/           # z. B. Paging/Sort-Clamping
├── Localization/   # ErrorMessages-Ressourcen
├── Mappers/        # AutoMapper-Profile
├── Providers/      # SpecimenExcelProvider (Im-/Export)
└── Utils/          # FuzzyMatch
```

### Integrationstests (`Zootify.Tests.Integration`)

**Docker muss laufen.** Pro Testlauf wird via **Testcontainers** ein echter SQL Server gestartet; zwischen
den Tests wird die DB mit **Respawn** zurückgesetzt. Die App wird über eine `WebApplicationFactory`
(`ZootifyTestFactory`) gehostet, externe E-Mail (Brevo) ist gemockt – es ist **kein echter API-Key** nötig
(die Test-`config.json` enthält bereits Platzhalter). Framework: **xUnit + Testcontainers.MsSql + Respawn +
FluentAssertions**.

```bash
dotnet test Zootify.Tests.Integration
```

Struktur (Auszug):

```text
Zootify.Tests.Integration/
├── Admin/          # Admin-/Moderator-Controller (Loans, User, Taxonomie, Notifications, E-Mail-Sprache)
├── Auth/           # AuthController (Login, Registrierung, Reset)
├── Common/         # Specimen, Collection, Tag, Taxonomy, User, Settings, Im-/Export, Bilder
├── Home/           # Öffentliche Übersicht
├── Jobs/           # Purge- und Loan-Reminder-Services
├── Localization/   # Sprach-/Fehlertexte
└── Utils/
    ├── ZootifyTestFactory.cs       # WebApplicationFactory mit Testcontainers-DB
    ├── IntegrationTestFixture.cs   # Container-Lifecycle + Respawn
    ├── IntegrationTestBase.cs      # Basisklasse für Tests
    └── Helpers/                    # Test-Repositories und Konfiguration
```

### Gemeinsame Test-Utilities (`Zootify.Tests.Shared`)

Builder und Testdaten (z. B. `UserEntityBuilder`, `UserTestData`), die von Unit- und Integrationstests
genutzt werden.

### Alle Tests auf einmal

```bash
dotnet test
```

### Continuous Integration (GitHub Actions)

Die Pipeline liegt unter `.github/workflows/ci.yml` und läuft bei jedem Push und Pull-Request (alle
Branches). Sie hat vier Jobs auf `ubuntu-latest` (Reihenfolge: Build → Docker Build → Tests):

1. **Build** – `dotnet build` der gesamten Solution (Release).
2. **Docker Build** – `docker build` des API-Images aus dem `Dockerfile`; schlägt fehl, wenn das Image
   nicht baut.
3. **Unit Tests** – `dotnet test Zootify.Tests.Unit`.
4. **Integration Tests** – `dotnet test Zootify.Tests.Integration` (Testcontainers; Docker ist auf den
   GitHub-Runnern vorinstalliert).

Die beiden Test-Jobs hängen am **Docker-Build** (`needs: docker-build`), laufen also erst, wenn das Image
erfolgreich gebaut wurde.

Wichtig: Die CI braucht **keine Secrets und keine `.env`**. Die Integrationstests sind self-contained –
sie starten ihren eigenen SQL Server (Testcontainers, Schema via `EnsureCreated`, Respawn zwischen den
Tests) und mocken externe Dienste (Brevo, Nominatim, SignalR); JWT-/Test-Werte kommen aus der
Test-Konfiguration. Da die `.env` gitignored ist, existiert sie in CI gar nicht – der `DotEnv`-Loader ist
dort also ohnehin wirkungslos. Zusätzlich lädt `DotEnv` grundsätzlich **nicht** unter dem Test-Host, sodass
auch eine lokal vorhandene `.env` die Tests nie beeinflusst.

---

## Technologie-Stack

| Bereich | Technologie |
|---------|-------------|
| Runtime | .NET 10 |
| Framework | ASP.NET Core (REST-API) |
| ORM | Entity Framework Core 10 |
| Datenbank | Microsoft SQL Server (Schema `zootify`) |
| Migrationen | Evolve (SQL-Skript-basiert) |
| DI-Container | Autofac (+ Castle DynamicProxy für Transaktionen) |
| Mapping | AutoMapper |
| Auth | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) |
| Passwort-Hashing | BCrypt.Net-Next |
| E-Mail | Brevo (SendWithBrevo) |
| Geocoding | OpenStreetMap Nominatim |
| Echtzeit | SignalR |
| Excel Im-/Export | ClosedXML |
| API-Dokumentation | Swagger / OpenAPI (Swashbuckle) |
| Unit-Tests | xUnit + Moq + FluentAssertions |
| Integrationstests | xUnit + Testcontainers (SQL Server) + Respawn + FluentAssertions |

---

## Lizenz

Dieses Projekt steht unter der **MIT-Lizenz** – Nutzung, Änderung und Weitergabe sind erlaubt, solange der
Copyright- und Lizenzhinweis erhalten bleibt. Den vollständigen Text finden Sie in der Datei
[`LICENSE`](LICENSE).

Copyright (c) 2026 Flakron Demaj
