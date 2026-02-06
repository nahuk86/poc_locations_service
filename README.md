# POC Locations Service (ASP.NET Core .NET 8 + SQL Server)

A lightweight microservice that centralizes **location datasets** consumed by client-facing web sites (e.g., map locator widgets).  
It provides a **single source of truth** for locations and supports filtering by **country** and/or **map**, plus **geospatial queries** (bbox / radius).

> This repository is a Proof of Concept / MVP. Authentication & authorization are intentionally out of scope at this stage.

---

## Key Use Cases Covered

### Reference data management
- ✅ Create / update **Countries**
- ✅ Create / update **Maps**
- ✅ List all **Countries** (discover `country_id`)
- ✅ List all **Maps** (discover `map_id`)

### Location dataset management
- ✅ Create **Locations**
- ✅ Update **Locations** (PATCH)
- ✅ List all Locations (admin endpoint, paginated)
- ✅ Retrieve all Location IDs (for bulk operations)

### Dataset relationships (many-to-many)
- ✅ Add locations to a **Country** (bulk associate)
- ✅ Add locations to a **Map** (bulk associate)
- ✅ Remove locations from a Country/Map (**soft remove** via association status / `REPLACE` mode)
- ✅ Query locations by **Country**
- ✅ Query locations by **Map**

### Geospatial querying (public endpoint)
- ✅ Query locations by **bounding box** (bbox)
- ✅ Query locations by **radius** (lat/lng + radius_km)
- ✅ Optional pre-filtering by `country_id` and/or `map_id` for performance

---

## Architecture Overview

**ASP.NET Core Web API (.NET 8)** + **Entity Framework Core** + **SQL Server** (tested with SQL Server Express).  
Geospatial support is enabled through **NetTopologySuite** with SQL Server `geography` type.

### High-level components
- **Controllers**
  - Admin endpoints for CRUD and dataset management
  - Public endpoint for geo queries (intended for client-facing sites)
- **Data layer**
  - EF Core entities mapped to SQL Server tables
  - Many-to-many relationships:
    - Location ↔ Country
    - Location ↔ Map
- **DTO-based responses**
  - Admin endpoints return DTOs (e.g., `AdminLocationDto`) to avoid serializing geospatial objects (`geom`).

---

## Data Model

### Core tables

#### `countries`
- `country_id` (PK, string)
- `name`
- `status` (ACTIVE / INACTIVE / DELETED)
- `created_at`, `updated_at`

#### `maps`
- `map_id` (PK, string)
- `name`
- `description`
- `status` (ACTIVE / INACTIVE / DELETED)
- `created_at`, `updated_at`

#### `locations`
- `location_id` (PK, GUID)
- `external_reference` (optional)
- Address fields: `name`, `address_line1`, `address_line2`, `city`, `region`, `postal_code`
- Contact fields: `phone`, `email`, `website_url`, `hours_text`
- Coordinates:
  - `latitude` (decimal)
  - `longitude` (decimal)
  - `geom` (SQL Server `geography` Point, SRID 4326)
- `status` (ACTIVE / INACTIVE / DELETED)
- `created_at`, `updated_at`

### Join tables (many-to-many)

#### `location_country`
- Composite PK: (`location_id`, `country_id`)
- `status` (ACTIVE / INACTIVE / DELETED)
- `created_at`, `updated_at`

#### `location_map`
- Composite PK: (`location_id`, `map_id`)
- `status` (ACTIVE / INACTIVE / DELETED)
- `created_at`, `updated_at`

> **Soft delete approach**: removals are represented by `status = INACTIVE` (rather than physically deleting rows).

---

## Running Locally

### Prerequisites
- .NET SDK 8.x
- SQL Server Express (or any SQL Server instance)

### Configure the connection string
Edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Db": "Server=YOUR_SERVER\\SQLEXPRESS;Database=db_poc_ms_locations;User Id=serviceaccount;Password=serviceaccount;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}

### Run the API

```bash
dotnet restore
dotnet run
```

Swagger is typically available at:

* `https://localhost:{PORT}/swagger`

---

## Optional: CORS (for a static HTML/JS Admin UI)

If you are building a local HTML/JS UI to call the API, enable CORS in `Program.cs`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("ui", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseCors("ui");
```

---

## Seed / Dummy Data

This project includes a SQL seed script to load:

* Countries
* Maps
* Locations
* Relationships (Location ↔ Country, Location ↔ Map)

Suggested workflow:

1. Create database + tables
2. Run seed script
3. Call API endpoints to validate queries and bulk associations

---

## API Endpoints (Summary)

> Endpoint names may vary slightly depending on your branch/version. Refer to Swagger for the full contract.

### Health

* `GET /v1/health`
* `GET /v1/db-ping`

### Public query (client-facing)

* `GET /v1/locations`

  * Supports:

    * `bbox=minLng,minLat,maxLng,maxLat`
    * `lat`, `lng`, `radius_km`
  * Optional filters:

    * `map_id`
    * `country_id`
  * Pagination:

    * `limit`
    * `offset`

Example (radius):

```bash
curl -X GET "https://localhost:7125/v1/locations?map_id=respiratory&country_id=MX&lat=19.4326&lng=-99.1332&radius_km=10&limit=100&offset=0"
```

Example (bbox):

```bash
curl -X GET "https://localhost:7125/v1/locations?map_id=respiratory&country_id=MX&bbox=-99.3,19.3,-99.0,19.5&limit=100&offset=0"
```

### Admin: Countries

* `GET /v1/countries?status=ACTIVE`
* `POST /v1/admin/countries`
* `GET /v1/admin/countries/{countryId}`
* `PATCH /v1/admin/countries/{countryId}`

### Admin: Maps

* `GET /v1/maps?status=ACTIVE`
* `POST /v1/admin/maps`
* `GET /v1/admin/maps/{mapId}`
* `PATCH /v1/admin/maps/{mapId}`

### Admin: Locations

* `GET /v1/admin/locations?status=ACTIVE&country_id=MX&map_id=respiratory&limit=100&offset=0`
* `GET /v1/admin/locations/ids?status=ACTIVE&limit=5000&offset=0`
* `GET /v1/admin/locations/{locationId}`
* `POST /v1/admin/locations`
* `POST /v1/admin/locations/bulk` - Create multiple locations in a single request
* `PATCH /v1/admin/locations/{locationId}`

Bulk create payload example:

```json
{
  "locations": [
    {
      "externalReference": "LOC-001",
      "name": "Sample Location 1",
      "addressLine1": "123 Main St",
      "addressLine2": "Suite 100",
      "city": "Mexico City",
      "region": "CDMX",
      "postalCode": "01000",
      "latitude": 19.4326,
      "longitude": -99.1332,
      "phone": "+52 55 1234 5678",
      "email": "contact@location1.com",
      "websiteUrl": "https://location1.com",
      "hoursText": "Mon-Fri 9AM-5PM",
      "countries": ["MX"],
      "maps": ["respiratory"]
    },
    {
      "externalReference": "LOC-002",
      "name": "Sample Location 2",
      "addressLine1": "456 Elm Ave",
      "city": "Guadalajara",
      "region": "Jalisco",
      "postalCode": "44100",
      "latitude": 20.6597,
      "longitude": -103.3496,
      "phone": "+52 33 1234 5678",
      "countries": ["MX"]
    }
  ]
}
```

### Admin: Bulk association (relationships)

* `POST /v1/admin/countries/{countryId}/locations:bulkAssociate`
* `POST /v1/admin/maps/{mapId}/locations:bulkAssociate`

Payload example:

```json
{
  "mode": "MERGE",
  "items": [
    { "locationId": "4e39a813-6f0b-4253-8f8a-0c1063c08366", "status": "ACTIVE" }
  ]
}
```

**Modes**

* `MERGE`: upserts the provided relations and keeps the rest unchanged
* `REPLACE`: sets provided relations to ACTIVE (or provided status) and marks all other existing relations as INACTIVE

---

## Technical Notes

### 1) DTOs for admin endpoints (avoid geospatial JSON issues)

Entities include a geospatial field (`geom`) backed by NetTopologySuite.
Admin endpoints return DTOs and exclude `geom` to avoid JSON serialization issues (Infinity/NaN from internal geometry fields).

### 2) SQL Server triggers + EF Core OUTPUT clause

If SQL tables have triggers enabled, EF Core may fail inserts/updates due to SQL Server restrictions with `OUTPUT` clauses.
Mitigation: configure EF mappings for join tables:

```csharp
modelBuilder.Entity<LocationCountry>(e =>
{
    e.ToTable("location_country", "dbo", tb => tb.UseSqlOutputClause(false));
});

modelBuilder.Entity<LocationMap>(e =>
{
    e.ToTable("location_map", "dbo", tb => tb.UseSqlOutputClause(false));
});
```

### 3) Soft deletes

Deletion is represented through `status` rather than physical deletes:

* `ACTIVE`: visible / usable
* `INACTIVE`: removed from a dataset but kept for history
* `DELETED`: hard-disabled (reserved for future rules)

---

## Roadmap / Next Steps

* Authentication & authorization (role-based admin operations)
* Contract hardening:

  * consistent error responses
  * validation & constraints
  * DTO standardization across all endpoints
* Cursor-based export (download the full dataset efficiently without offset limitations)
* Audit fields (created_by / updated_by) and optional publishing workflow
* Docker compose for local environment (API + SQL Server)

---

