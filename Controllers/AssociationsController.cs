using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using poc_locations_service.Data;
using poc_locations_service.Dtos;
using poc_locations_service.Helpers;

namespace poc_locations_service.Controllers
{
    [ApiController]

    public class AssociationsController : Controller
    {
        private readonly AppDbContext _db;
        public AssociationsController(AppDbContext db) => _db = db;

        // POST /v1/admin/maps/{mapId}/locations:bulkAssociate
        [HttpPost("v1/admin/maps/{mapId}/locations:bulkAssociate")]
        public async Task<IActionResult> BulkAssociateMap(string mapId, [FromBody] BulkAssociateRequest req)
        {
            var mode = GeoHelper.NormalizeMode(req.Mode);
            var mapExists = await _db.Maps.AnyAsync(m => m.MapId == mapId);
            if (!mapExists) return NotFound(new { error = "map not found" });

            var ids = req.Items.Select(x => x.LocationId).Distinct().ToList();

            // MERGE: upsert associations for provided ids
            var existing = await _db.LocationMaps.Where(x => x.MapId == mapId && ids.Contains(x.LocationId)).ToListAsync();

            foreach (var item in req.Items)
            {
                var status = (item.Status ?? RecordStatuses.ACTIVE).ToUpperInvariant();
                var rel = existing.FirstOrDefault(x => x.LocationId == item.LocationId);
                if (rel is null)
                {
                    _db.LocationMaps.Add(new LocationMap
                    {
                        MapId = mapId,
                        LocationId = item.LocationId,
                        Status = status,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    rel.Status = status;
                    rel.UpdatedAt = DateTime.UtcNow;
                }
            }

            // REPLACE: deactivate any existing association not in ids
            if (mode == "REPLACE")
            {
                var toDeactivate = await _db.LocationMaps.Where(x => x.MapId == mapId && !ids.Contains(x.LocationId)).ToListAsync();
                foreach (var rel in toDeactivate)
                {
                    rel.Status = RecordStatuses.INACTIVE;
                    rel.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _db.SaveChangesAsync();
            return Ok(new { map_id = mapId, mode, processed = req.Items.Count });
        }

        // POST /v1/admin/countries/{countryId}/locations:bulkAssociate
        [HttpPost("v1/admin/countries/{countryId}/locations:bulkAssociate")]
        public async Task<IActionResult> BulkAssociateCountry(string countryId, [FromBody] BulkAssociateRequest req)
        {
            var mode = GeoHelper.NormalizeMode(req.Mode);
            var countryExists = await _db.Countries.AnyAsync(c => c.CountryId == countryId);
            if (!countryExists) return NotFound(new { error = "country not found" });

            var ids = req.Items.Select(x => x.LocationId).Distinct().ToList();

            var existing = await _db.LocationCountries.Where(x => x.CountryId == countryId && ids.Contains(x.LocationId)).ToListAsync();

            foreach (var item in req.Items)
            {
                var status = (item.Status ?? RecordStatuses.ACTIVE).ToUpperInvariant();
                var rel = existing.FirstOrDefault(x => x.LocationId == item.LocationId);
                if (rel is null)
                {
                    _db.LocationCountries.Add(new LocationCountry
                    {
                        CountryId = countryId,
                        LocationId = item.LocationId,
                        Status = status,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    rel.Status = status;
                    rel.UpdatedAt = DateTime.UtcNow;
                }
            }

            if (mode == "REPLACE")
            {
                var toDeactivate = await _db.LocationCountries.Where(x => x.CountryId == countryId && !ids.Contains(x.LocationId)).ToListAsync();
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
