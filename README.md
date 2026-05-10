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

Die Datenbank besteht aus 6 Tabellen:

---

#### Users (Benutzer)

Speichert alle registrierten Benutzer der Plattform.

| Spalte | Typ | Beschreibung |
|--------|-----|-------------|
| Id | int | Primaerschluessel, wird automatisch vergeben |
| FirstName | string (max 100) | Vorname |
| LastName | string (max 100) | Nachname |
| Email | string (max 256) | E-Mail-Adresse, muss einzigartig sein (fuer Login) |
| PasswordHash | string (max 512) | Gehashtes Passwort (nie im Klartext gespeichert) |
| Role | string | Rolle: `User`, `Moderator` oder `Admin` |
| Status | string | Kontostatus: `Pending` (wartet auf Aktivierung), `Active` (aktiv) oder `Blocked` (gesperrt) |
| CreatedAt | datetime | Zeitpunkt der Registrierung |

**Beziehungen:** Ein User kann mehrere Specimens besitzen, Taxonomien vorschlagen und an Ausleihen beteiligt sein.

---

#### TaxonomyNodes (SCOFGA-Hierarchie)

Bildet die biologische Klassifikation als Baumstruktur ab. Alle 6 Stufen (Stamm, Klasse, Ordnung, Familie, Gattung, Art) sind in einer Tabelle mit Parent-Child-Beziehung.

| Spalte | Typ | Beschreibung |
|--------|-----|-------------|
| Id | int | Primaerschluessel |
| Name | string (max 256) | Name des Taxons (z.B. "Insekten", "Schmetterlinge") |
| Rank | string | Hierarchie-Stufe: `Phylum` (Stamm), `Class` (Klasse), `Order` (Ordnung), `Family` (Familie), `Genus` (Gattung), `Species` (Art) |
| ApprovalStatus | string | Freigabe-Status: `Pending` (von Nutzer vorgeschlagen, wartet auf Moderator), `Approved` (freigegeben), `Rejected` (abgelehnt) |
| ParentId | int? | Verweis auf den uebergeordneten Eintrag (z.B. Klasse verweist auf ihren Stamm). Null bei Wurzel-Eintraegen (Staemme) |
| ProposedByUserId | int? | Welcher Nutzer diesen Eintrag vorgeschlagen hat (null bei System-Eintraegen) |

**Beispiel-Baum:**
```
Gliederf. (Phylum, ParentId=null)
  -> Insekten (Class, ParentId=1)
    -> Schmetterlinge (Order, ParentId=2)
      -> Tagfalter (Family, ParentId=3)
        -> Vanessa (Genus, ParentId=4)
          -> Vanessa cardui (Species, ParentId=5)
```

**Unique-Index:** Pro Eltern-Knoten darf es keine doppelten Namen auf gleicher Stufe geben.

---

#### GeographicLocations (Geographische Hierarchie)

Bildet politische/geographische Strukturen als Baumstruktur ab, fuer die strukturierte Suche nach Fundorten.

| Spalte | Typ | Beschreibung |
|--------|-----|-------------|
| Id | int | Primaerschluessel |
| Name | string (max 256) | Name des Ortes (z.B. "Deutschland", "NRW", "Siegen") |
| Level | string | Hierarchie-Ebene: `Continent`, `Country`, `State`, `District`, `City` |
| ParentId | int? | Verweis auf den uebergeordneten Eintrag. Null bei Kontinenten |

**Beispiel-Baum:**
```
Europa (Continent)
  -> Deutschland (Country)
    -> NRW (State)
      -> Siegen-Wittgenstein (District)
        -> Siegen (City)
```

---

#### Specimens (Funde / Individuen)

Das **Kernobjekt** der Sammlung. Jeder Eintrag repraesentiert ein gesammeltes Tier (z.B. ein Insekt).

| Spalte | Typ | Beschreibung |
|--------|-----|-------------|
| Id | int | Primaerschluessel |
| SpeciesId | int | Welche Art (Verweis auf TaxonomyNodes, muss eine Species sein) |
| GeographicLocationId | int? | Grobe Fundort-Zuordnung (Verweis auf GeographicLocations, z.B. "Siegen") |
| Latitude | double? | Exakter Breitengrad des Fundortes (z.B. 50.8748) - fuer die Kartenansicht |
| Longitude | double? | Exakter Laengengrad des Fundortes (z.B. 8.0243) - fuer die Kartenansicht |
| FindDate | datetime? | Wann wurde das Tier gefunden |
| OwnerId | int | Wem gehoert dieser Fund (Verweis auf Users) |
| StorageBuilding | string? (max 200) | Aufbewahrungsort: Gebaeude |
| StorageCabinet | string? (max 200) | Aufbewahrungsort: Schrank |
| StorageShelf | string? (max 200) | Aufbewahrungsort: Regal |
| StorageCompartment | string? (max 200) | Aufbewahrungsort: Fach |
| Comment | string? (max 2000) | Freitext-Kommentar |
| IsPublic | bool | Sichtbarkeit: true = oeffentlich sichtbar, false = nur fuer den Besitzer |
| CreatedAt | datetime | Wann der Eintrag erstellt wurde |
| UpdatedAt | datetime | Wann der Eintrag zuletzt geaendert wurde |

**Beziehungen:** Jeder Fund gehoert zu genau einem User (Owner), einer Art (Species) und optional einem Ort (GeographicLocation). Ein Fund kann mehrere Bilder und Ausleihen haben.

---

#### SpecimenImages (Bilder)

Speichert Bilder/Fotos zu einzelnen Funden (Wunschkriterium).

| Spalte | Typ | Beschreibung |
|--------|-----|-------------|
| Id | int | Primaerschluessel |
| SpecimenId | int | Zu welchem Fund gehoert das Bild (Verweis auf Specimens) |
| ImageUrl | string (max 1000) | Dateipfad oder URL zum Bild |
| UploadedAt | datetime | Wann das Bild hochgeladen wurde |

---

#### Loans (Ausleihen)

Dokumentiert das Verleihen und Aufbewahren von Sammlungsobjekten.

| Spalte | Typ | Beschreibung |
|--------|-----|-------------|
| Id | int | Primaerschluessel |
| SpecimenId | int | Welches Objekt wird verliehen (Verweis auf Specimens) |
| LenderId | int | Wer verleiht (Verweis auf Users) |
| BorrowerId | int? | Wer leiht aus, falls registrierter Nutzer (Verweis auf Users) |
| BorrowerExternalName | string? (max 256) | Name des Ausleihers, falls externe Person (nicht registriert) |
| LoanDate | datetime | Wann wurde verliehen |
| DueDate | datetime | Bis wann muss zurueckgegeben werden (fuer Erinnerungen/Notifications) |
| ReturnDate | datetime? | Wann tatsaechlich zurueckgegeben (null = noch ausgeliehen) |
| Notes | string? (max 2000) | Anmerkungen zur Ausleihe |

---

### Beziehungen (ER-Diagramm)

```
User 1──* Specimen *──1 TaxonomyNode
  |                       | (self-ref)
  |                       +-- Parent/Children
  |
  +-- 1──* Loan (als Lender)
  +-- 1──* Loan (als Borrower)
  +-- 1──* TaxonomyNode (als Vorschlag)

Specimen *──1 GeographicLocation
  |              | (self-ref)
  |              +-- Parent/Children
  +-- 1──* SpecimenImage
  +-- 1──* Loan
```

### Löschverhalten

| Beziehung | Verhalten | Grund |
|-----------|-----------|-------|
| TaxonomyNode -> Parent | **Restrict** | Kein Stamm loeschen wenn Klassen drunter haengen |
| GeographicLocation -> Parent | **Restrict** | Kein Land loeschen wenn Bundeslaender existieren |
| Specimen -> Species | **Restrict** | Keine Art loeschen die bei Funden verwendet wird |
| Specimen -> Owner (User) | **Cascade** | User geloescht = seine Funde werden mitgeloescht |
| Specimen -> GeographicLocation | **SetNull** | Ort geloescht = Fund bleibt, Ort-Referenz wird null |
| SpecimenImage -> Specimen | **Cascade** | Fund geloescht = Bilder werden mitgeloescht |
| Loan -> Specimen | **Cascade** | Fund geloescht = Ausleihen werden mitgeloescht |
| Loan -> Lender/Borrower | **Restrict** | Kein User loeschen der aktive Ausleihen hat |

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
