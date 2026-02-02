using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using poc_locations_service.Data;
using poc_locations_service.Dtos;
using poc_locations_service.Helpers;

namespace poc_locations_service.Controllers
{

    [ApiController]

    public class BulkUpsertController : Controller
    {
        private readonly AppDbContext _db;
        public BulkUpsertController(AppDbContext db) => _db = db;

        // PUT /v1/admin/maps/{mapId}/locations:bulkUpsert?mode=MERGE
        [HttpPut("v1/admin/maps/{mapId}/locations:bulkUpsert")]
        public async Task<IActionResult> BulkUpsertForMap(string mapId, [FromBody] BulkUpsertRequest req, [FromQuery] string mode = "MERGE")
        {
            mode = GeoHelper.NormalizeMode(mode);
            var mapExists = await _db.Maps.AnyAsync(m => m.MapId == mapId);
            if (!mapExists) return NotFound(new { error = "map not found" });

            var extRefs = req.Items.Select(i => i.ExternalReference).Distinct().ToList();
            var existing = await _db.Locations.Where(l => l.ExternalReference != null && extRefs.Contains(l.ExternalReference)).ToListAsync();

            var touchedLocationIds = new HashSet<Guid>();

            foreach (var item in req.Items)
            {
                var loc = existing.FirstOrDefault(x => x.ExternalReference == item.ExternalReference);
                if (loc is null)
                {
                    loc = new Location
                    {
                        LocationId = Guid.NewGuid(),
                        ExternalReference = item.ExternalReference,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.Locations.Add(loc);
                    existing.Add(loc);
                }

                // Update fields
                loc.Name = item.Name;
                loc.AddressLine1 = item.AddressLine1;
                loc.AddressLine2 = item.AddressLine2;
                loc.City = item.City;
                loc.Region = item.Region;
                loc.PostalCode = item.PostalCode;
                loc.Latitude = item.Latitude;
                loc.Longitude = item.Longitude;
                loc.Geom = GeoHelper.ToPoint(item.Latitude, item.Longitude);
                loc.Phone = item.Phone;
                loc.Email = item.Email;
                loc.WebsiteUrl = item.WebsiteUrl;
                loc.HoursText = item.HoursText;
                loc.Status = RecordStatuses.ACTIVE;
                loc.UpdatedAt = DateTime.UtcNow;

                touchedLocationIds.Add(loc.LocationId);

                // Ensure association to map
                var assocStatus = (item.AssociationStatus ?? RecordStatuses.ACTIVE).ToUpperInvariant();
                var rel = await _db.LocationMaps.FirstOrDefaultAsync(x => x.MapId == mapId && x.LocationId == loc.LocationId);
                if (rel is null)
                {
                    _db.LocationMaps.Add(new LocationMap
                    {
                        MapId = mapId,
                        LocationId = loc.LocationId,
                        Status = assocStatus,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    rel.Status = assocStatus;
                    rel.UpdatedAt = DateTime.UtcNow;
                }

                // Optionally ensure countries too
                if (item.Countries is not null)
                {
                    foreach (var cid in item.Countries.Distinct())
                    {
                        var lc = await _db.LocationCountries.FirstOrDefaultAsync(x => x.CountryId == cid && x.LocationId == loc.LocationId);
                        if (lc is null)
                        {
                            _db.LocationCountries.Add(new LocationCountry
                            {
                                CountryId = cid,
                                LocationId = loc.LocationId,
                                Status = RecordStatuses.ACTIVE,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            });
                        }
                        else
                        {
                            lc.Status = RecordStatuses.ACTIVE;
                            lc.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }
            }

            // REPLACE: deactivate associations in that map that were not included
            if (mode == "REPLACE")
            {
                var toDeactivate = await _db.LocationMaps.Where(x => x.MapId == mapId && !touchedLocationIds.Contains(x.LocationId)).ToListAsync();
                foreach (var rel in toDeactivate)
                {
                    rel.Status = RecordStatuses.INACTIVE;
                    rel.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _db.SaveChangesAsync();
            return Ok(new { map_id = mapId, mode, processed = req.Items.Count });
        }

        // PUT /v1/admin/countries/{countryId}/locations:bulkUpsert?mode=MERGE
        [HttpPut("v1/admin/countries/{countryId}/locations:bulkUpsert")]
        public async Task<IActionResult> BulkUpsertForCountry(string countryId, [FromBody] BulkUpsertRequest req, [FromQuery] string mode = "MERGE")
        {
            mode = GeoHelper.NormalizeMode(mode);
            var countryExists = await _db.Countries.AnyAsync(c => c.CountryId == countryId);
            if (!countryExists) return NotFound(new { error = "country not found" });

            var extRefs = req.Items.Select(i => i.ExternalReference).Distinct().ToList();
            var existing = await _db.Locations.Where(l => l.ExternalReference != null && extRefs.Contains(l.ExternalReference)).ToListAsync();

            var touchedLocationIds = new HashSet<Guid>();

            foreach (var item in req.Items)
            {
                var loc = existing.FirstOrDefault(x => x.ExternalReference == item.ExternalReference);
                if (loc is null)
                {
                    loc = new Location
                    {
                        LocationId = Guid.NewGuid(),
                        ExternalReference = item.ExternalReference,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.Locations.Add(loc);
                    existing.Add(loc);
                }

                loc.Name = item.Name;
                loc.AddressLine1 = item.AddressLine1;
                loc.AddressLine2 = item.AddressLine2;
                loc.City = item.City;
                loc.Region = item.Region;
                loc.PostalCode = item.PostalCode;
                loc.Latitude = item.Latitude;
                loc.Longitude = item.Longitude;
                loc.Geom = GeoHelper.ToPoint(item.Latitude, item.Longitude);
                loc.Phone = item.Phone;
                loc.Email = item.Email;
                loc.WebsiteUrl = item.WebsiteUrl;
                loc.HoursText = item.HoursText;
                loc.Status = RecordStatuses.ACTIVE;
                loc.UpdatedAt = DateTime.UtcNow;

                touchedLocationIds.Add(loc.LocationId);

                var assocStatus = (item.AssociationStatus ?? RecordStatuses.ACTIVE).ToUpperInvariant();

                // Ensure association to the countryId from route (primary for this operation)
                var rel = await _db.LocationCountries.FirstOrDefaultAsync(x => x.CountryId == countryId && x.LocationId == loc.LocationId);
                if (rel is null)
                {
                    _db.LocationCountries.Add(new LocationCountry
                    {
                        CountryId = countryId,
                        LocationId = loc.LocationId,
                        Status = assocStatus,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    rel.Status = assocStatus;
                    rel.UpdatedAt = DateTime.UtcNow;
                }
            }

            if (mode == "REPLACE")
            {
                var toDeactivate = await _db.LocationCountries.Where(x => x.CountryId == countryId && !touchedLocationIds.Contains(x.LocationId)).ToListAsync();
                foreach (var rel in toDeactivate)
                {
                    rel.Status = RecordStatuses.INACTIVE;
                    rel.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _db.SaveChangesAsync();
            return Ok(new { country_id = countryId, mode, processed = req.Items.Count });
        }
    }
}
