# Zootify Backend

Backend-API fuer die Plattform zur Dokumentation von Zoologischen Sammlungen.  
Entwickelt mit **ASP.NET Core** und **Entity Framework Core** (SQL Server).

## Voraussetzungen

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/de-de/sql-server/) (Express reicht aus)
- EF Core Tools: `dotnet tool install --global dotnet-ef`

## Projekt starten

### 1. Connection-String anpassen

In `appsettings.json` den Server-Namen anpassen, falls noetig:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=ZootifyDb;Trusted_Connection=True;TrustServerCertificate=True"
}
```

Beispiele:
- SQL Server Express: `Server=.\SQLEXPRESS;...`
- Lokaler Server mit Instanzname: `Server=localhost\MEINSERVER;...`

### 2. Datenbank erstellen

```bash
dotnet ef database update
```

### 3. App starten

```bash
dotnet run
```

## Projektstruktur

```
Zootify_Backend/
├── Enums/                    # Aufzaehlungstypen (Rollen, Status, etc.)
│   ├── ApprovalStatus.cs     # Pending, Approved, Rejected
│   ├── GeographicLevel.cs    # Continent, Country, State, District, City
│   ├── TaxonomyRank.cs       # Phylum, Class, Order, Family, Genus, Species
│   ├── UserRole.cs           # User, Moderator, Admin
│   └── UserStatus.cs         # Pending, Active, Blocked
├── Models/                   # Datenbank-Tabellen als C#-Klassen
│   ├── User.cs               # Benutzer mit Rolle und Status
│   ├── TaxonomyNode.cs       # SCOFGA-Hierarchie (Stamm bis Art)
│   ├── GeographicLocation.cs # Geographische Hierarchie (Kontinent bis Stadt)
│   ├── Specimen.cs           # Kernobjekt: Fund/Individuum
│   ├── SpecimenImage.cs      # Bilder pro Individuum
│   └── Loan.cs               # Ausleih-Management
├── Data/
│   └── ZootifyDbContext.cs   # EF Core Konfiguration (Beziehungen, Indizes)
├── Migrations/               # Auto-generiert von EF Core
├── Program.cs                # App-Startpunkt
└── appsettings.json          # Konfiguration (Connection-String)
```

## Datenmodell

### Tabellen-Uebersicht

| Tabelle | Beschreibung |
|---------|-------------|
| **Users** | Benutzer mit E-Mail (unique), Passwort-Hash, Rolle und Status |
| **TaxonomyNodes** | SCOFGA-Hierarchie als Parent-Child-Baum |
| **GeographicLocations** | Geographische Hierarchie als Parent-Child-Baum |
| **Specimens** | Funde mit Taxonomie, Koordinaten, Aufbewahrungsort, Sichtbarkeit |
| **SpecimenImages** | Bilder zu Funden |
| **Loans** | Ausleihen mit Fristen |

### Beziehungen

```
User 1──* Specimen *──1 TaxonomyNode
  │                       │ (self-ref)
  │                       └── Parent/Children
  │
  ├── 1──* Loan (als Lender)
  ├── 1──* Loan (als Borrower)
  └── 1──* TaxonomyNode (als Vorschlag)

Specimen *──1 GeographicLocation
  │              │ (self-ref)
  │              └── Parent/Children
  ├── 1──* SpecimenImage
  └── 1──* Loan
```

### Taxonomie (SCOFGA)

Alle 6 Stufen in einer Tabelle mit Parent-Child-Beziehung:

```
Gliederf. (Phylum) -> Insekten (Class) -> Schmetterlinge (Order)
  -> Tagfalter (Family) -> Vanessa (Genus) -> Vanessa cardui (Species)
```

Neue Arten von Nutzern haben `ApprovalStatus = Pending` bis ein Moderator sie freigibt.

### Geographische Hierarchie

```
Europa (Continent) -> Deutschland (Country) -> NRW (State)
  -> Siegen-Wittgenstein (District) -> Siegen (City)
```

### Specimen (Kernobjekt)

Jeder Fund speichert:
- **Art** (Referenz auf TaxonomyNode)
- **Fundort** (Referenz auf GeographicLocation + exakte Koordinaten)
- **Funddatum**
- **Besitzer** (Referenz auf User)
- **Aufbewahrungsort** (Gebaeude, Schrank, Regal, Fach)
- **Sichtbarkeit** (oeffentlich/privat)
- **Kommentar**

## Neue Migration erstellen

Wenn sich das Datenmodell aendert:

```bash
dotnet ef migrations add BeschreibungDerAenderung
dotnet ef database update
```

## Technologie-Stack

- **Runtime:** .NET 10
- **Framework:** ASP.NET Core (REST-API)
- **ORM:** Entity Framework Core
- **Datenbank:** Microsoft SQL Server
- **API-Docs:** OpenAPI
