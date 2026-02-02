using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using poc_locations_service.Data;
using poc_locations_service.Dtos;
using poc_locations_service.Helpers;

namespace poc_locations_service.Controllers
{
    [ApiController]
    [Route("v1/admin/locations")]
    public class LocationsAdminController : Controller
    {
        private readonly AppDbContext _db;
        public LocationsAdminController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] string status = "ACTIVE",
            [FromQuery] string? map_id = null,
            [FromQuery] string? country_id = null,
            [FromQuery] DateTime? updated_since = null,
            [FromQuery] int limit = 1000,
            [FromQuery] int offset = 0)
        {
            limit = Math.Clamp(limit, 1, 5000);
            offset = Math.Max(offset, 0);

            var s = status.ToUpperInvariant();

            var q = _db.Locations.AsNoTracking().Where(l => l.Status == s);

            if (updated_since.HasValue)
                q = q.Where(l => l.UpdatedAt >= updated_since.Value);

            if (!string.IsNullOrWhiteSpace(map_id))
                q = q.Where(l => l.LocationMaps.Any(lm => lm.MapId == map_id && lm.Status == RecordStatuses.ACTIVE));

            if (!string.IsNullOrWhiteSpace(country_id))
                q = q.Where(l => l.LocationCountries.Any(lc => lc.CountryId == country_id && lc.Status == RecordStatuses.ACTIVE));

            var items = await q.OrderBy(l => l.LocationId).Skip(offset).Take(limit).ToListAsync();
            return Ok(new { data = items, meta = new { limit, offset, count = items.Count } });
        }

        [HttpGet("ids")]
        public async Task<IActionResult> ListIds([FromQuery] string status = "ACTIVE", [FromQuery] int limit = 5000, [FromQuery] int offset = 0)
        {
            limit = Math.Clamp(limit, 1, 5000);
            offset = Math.Max(offset, 0);

            var s = status.ToUpperInvariant();

            var ids = await _db.Locations.AsNoTracking()
                .Where(l => l.Status == s)
                .OrderBy(l => l.LocationId)
                .Skip(offset).Take(limit)
                .Select(l => l.LocationId)
                .ToListAsync();

            return Ok(new { data = ids, meta = new { limit, offset, count = ids.Count } });
        }

        [HttpGet("{locationId:guid}")]
        public async Task<IActionResult> Get(Guid locationId)
        {
            var loc = await _db.Locations.AsNoTracking()
                .Where(x => x.LocationId == locationId)
                .Select(x => new
                {
                    location_id = x.LocationId,
                    external_reference = x.ExternalReference,
                    name = x.Name,
                    address_line1 = x.AddressLine1,
                    address_line2 = x.AddressLine2,
                    city = x.City,
                    region = x.Region,
                    postal_code = x.PostalCode,
                    latitude = x.Latitude,
                    longitude = x.Longitude,
                    phone = x.Phone,
                    email = x.Email,
                    website_url = x.WebsiteUrl,
                    hours_text = x.HoursText,
                    status = x.Status,
                    created_at = x.CreatedAt,
                    updated_at = x.UpdatedAt
                })
                .FirstOrDefaultAsync();

            return loc is null ? NotFound() : Ok(loc);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateLocationRequest req)
        {
            var loc = new Location
            {
                LocationId = Guid.NewGuid(),
                ExternalReference = req.ExternalReference,
                Name = req.Name,
                AddressLine1 = req.AddressLine1,
                AddressLine2 = req.AddressLine2,
                City = req.City,
                Region = req.Region,
                PostalCode = req.PostalCode,
                Latitude = req.Latitude,
                Longitude = req.Longitude,
                Geom = GeoHelper.ToPoint(req.Latitude, req.Longitude),
                Phone = req.Phone,
                Email = req.Email,
                WebsiteUrl = req.WebsiteUrl,
                HoursText = req.HoursText,
                Status = RecordStatuses.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Locations.Add(loc);

            if (req.Countries is not null)
            {
                foreach (var cid in req.Countries.Distinct())
                {
                    _db.LocationCountries.Add(new LocationCountry
                    {
                        LocationId = loc.LocationId,
                        CountryId = cid,
                        Status = RecordStatuses.ACTIVE,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            if (req.Maps is not null)
            {
                foreach (var mid in req.Maps.Distinct())
                {
                    _db.LocationMaps.Add(new LocationMap
                    {
                        LocationId = loc.LocationId,
                        MapId = mid,
                        Status = RecordStatuses.ACTIVE,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            await _db.SaveChangesAsync();
            return Created($"/v1/admin/locations/{loc.LocationId}", new { location_id = loc.LocationId });
        }

        [HttpPatch("{locationId:guid}")]
        public async Task<IActionResult> Patch(Guid locationId, [FromBody] PatchLocationRequest req)
        {
            var loc = await _db.Locations.FirstOrDefaultAsync(x => x.LocationId == locationId);
            if (loc is null) return NotFound();

            if (req.ExternalReference is not null) loc.ExternalReference = req.ExternalReference;
            if (req.Name is not null) loc.Name = req.Name;
            if (req.AddressLine1 is not null) loc.AddressLine1 = req.AddressLine1;
            if (req.AddressLine2 is not null) loc.AddressLine2 = req.AddressLine2;
            if (req.City is not null) loc.City = req.City;
            if (req.Region is not null) loc.Region = req.Region;
            if (req.PostalCode is not null) loc.PostalCode = req.PostalCode;
            if (req.Phone is not null) loc.Phone = req.Phone;
            if (req.Email is not null) loc.Email = req.Email;
            if (req.WebsiteUrl is not null) loc.WebsiteUrl = req.WebsiteUrl;
            if (req.HoursText is not null) loc.HoursText = req.HoursText;
            if (!string.IsNullOrWhiteSpace(req.Status)) loc.Status = req.Status.Trim().ToUpperInvariant();

            if (req.Latitude.HasValue && req.Longitude.HasValue)
            {
                loc.Latitude = req.Latitude.Value;
                loc.Longitude = req.Longitude.Value;
                loc.Geom = GeoHelper.ToPoint(loc.Latitude, loc.Longitude);
            }

            loc.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { location_id = loc.LocationId, status = loc.Status });
        }
    }
}
