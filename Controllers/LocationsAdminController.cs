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

        public LocationsAdminController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] string status = "ACTIVE",
            [FromQuery] string? country_id = null,
            [FromQuery] string? map_id = null,
            [FromQuery] int limit = 100,
            [FromQuery] int offset = 0,
            [FromQuery] DateTime? updated_since = null
        )
        {
            limit = Math.Clamp(limit, 1, 5000);
            offset = Math.Max(0, offset);

            var q = _db.Locations.AsNoTracking()
                .Where(x => x.Status == status);

            if (updated_since.HasValue)
                q = q.Where(x => x.UpdatedAt >= updated_since.Value);

            if (!string.IsNullOrWhiteSpace(country_id))
            {
                q = q.Where(x => x.LocationCountries.Any(lc =>
                    lc.CountryId == country_id && lc.Status == "ACTIVE"));
            }

            if (!string.IsNullOrWhiteSpace(map_id))
            {
                q = q.Where(x => x.LocationMaps.Any(lm =>
                    lm.MapId == map_id && lm.Status == "ACTIVE"));
            }

            var total = await q.CountAsync();

            var data = await q
                .OrderByDescending(x => x.UpdatedAt)
                .Skip(offset)
                .Take(limit)
                .Select(x => new AdminLocationDto
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
                .ToListAsync();

            return Ok(new
            {
                data,
                paging = new { limit, offset, total }
            });
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

        [HttpPost("bulk")]
        public async Task<IActionResult> BulkCreate([FromBody] BulkCreateLocationsRequest req)
        {
            if (req.Locations == null || req.Locations.Count == 0)
            {
                return BadRequest(new { error = "Locations list cannot be empty" });
            }

            var createdIds = new List<Guid>();
            var now = DateTime.UtcNow;
            var locations = new List<Location>();
            var locationCountries = new List<LocationCountry>();
            var locationMaps = new List<LocationMap>();

            foreach (var locReq in req.Locations)
            {
                var loc = new Location
                {
                    LocationId = Guid.NewGuid(),
                    ExternalReference = locReq.ExternalReference,
                    Name = locReq.Name,
                    AddressLine1 = locReq.AddressLine1,
                    AddressLine2 = locReq.AddressLine2,
                    City = locReq.City,
                    Region = locReq.Region,
                    PostalCode = locReq.PostalCode,
                    Latitude = locReq.Latitude,
                    Longitude = locReq.Longitude,
                    Geom = GeoHelper.ToPoint(locReq.Latitude, locReq.Longitude),
                    Phone = locReq.Phone,
                    Email = locReq.Email,
                    WebsiteUrl = locReq.WebsiteUrl,
                    HoursText = locReq.HoursText,
                    Status = RecordStatuses.ACTIVE,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                locations.Add(loc);

                if (locReq.Countries is not null)
                {
                    foreach (var cid in locReq.Countries.Distinct())
                    {
                        locationCountries.Add(new LocationCountry
                        {
                            LocationId = loc.LocationId,
                            CountryId = cid,
                            Status = RecordStatuses.ACTIVE,
                            CreatedAt = now,
                            UpdatedAt = now
                        });
                    }
                }

                if (locReq.Maps is not null)
                {
                    foreach (var mid in locReq.Maps.Distinct())
                    {
                        locationMaps.Add(new LocationMap
                        {
                            LocationId = loc.LocationId,
                            MapId = mid,
                            Status = RecordStatuses.ACTIVE,
                            CreatedAt = now,
                            UpdatedAt = now
                        });
                    }
                }

                createdIds.Add(loc.LocationId);
            }

            _db.Locations.AddRange(locations);
            _db.LocationCountries.AddRange(locationCountries);
            _db.LocationMaps.AddRange(locationMaps);

            await _db.SaveChangesAsync();
            return Ok(new { created_count = createdIds.Count, location_ids = createdIds });
        }
    }
}
