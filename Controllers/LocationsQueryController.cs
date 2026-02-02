using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using poc_locations_service.Data;
using poc_locations_service.Helpers;

namespace poc_locations_service.Controllers
{
    [ApiController]
    [Route("v1/locations")]
    public class LocationsQueryController : Controller
    {
        private readonly AppDbContext _db;
        public LocationsQueryController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Query(
            [FromQuery] string? map_id,
            [FromQuery] string? country_id,
            [FromQuery] string? bbox,
            [FromQuery] double? lat,
            [FromQuery] double? lng,
            [FromQuery] double? radius_km,
            [FromQuery] int limit = 1000,
            [FromQuery] int offset = 0)
        {
            limit = Math.Clamp(limit, 1, 5000);
            offset = Math.Max(offset, 0);

            var hasBbox = !string.IsNullOrWhiteSpace(bbox);
            var hasRadius = lat.HasValue && lng.HasValue && radius_km.HasValue;

            if (hasBbox == hasRadius)
                return BadRequest(new { error = "Provide either bbox OR (lat,lng,radius_km)." });

            var q = _db.Locations.AsNoTracking()
                .Where(l => l.Status == RecordStatuses.ACTIVE);

            if (!string.IsNullOrWhiteSpace(map_id))
                q = q.Where(l => l.LocationMaps.Any(lm => lm.MapId == map_id && lm.Status == RecordStatuses.ACTIVE));

            if (!string.IsNullOrWhiteSpace(country_id))
                q = q.Where(l => l.LocationCountries.Any(lc => lc.CountryId == country_id && lc.Status == RecordStatuses.ACTIVE));

            if (hasBbox)
            {
                if (!GeoHelper.TryParseBbox(bbox!, out var minLng, out var minLat, out var maxLng, out var maxLat))
                    return BadRequest(new { error = "Invalid bbox format. Expected minLng,minLat,maxLng,maxLat." });

                var poly = GeoHelper.BboxToPolygon(minLng, minLat, maxLng, maxLat);
                q = q.Where(l => poly.Intersects(l.Geom)); // bbox filter
            }
            else
            {
                var center = GeoHelper.ToPoint((decimal)lat!.Value, (decimal)lng!.Value);
                var meters = radius_km!.Value * 1000.0;
                q = q.Where(l => l.Geom.Distance(center) <= meters);
            }

            var items = await q.OrderBy(l => l.LocationId)
                .Skip(offset).Take(limit)
                .Select(l => new
                {
                    location_id = l.LocationId,
                    name = l.Name,
                    address_line1 = l.AddressLine1,
                    address_line2 = l.AddressLine2,
                    city = l.City,
                    region = l.Region,
                    postal_code = l.PostalCode,
                    latitude = l.Latitude,
                    longitude = l.Longitude,
                    phone = l.Phone,
                    email = l.Email,
                    website_url = l.WebsiteUrl,
                    hours_text = l.HoursText,
                    maps = l.LocationMaps.Where(x => x.Status == RecordStatuses.ACTIVE).Select(x => x.MapId).ToArray(),
                    countries = l.LocationCountries.Where(x => x.Status == RecordStatuses.ACTIVE).Select(x => x.CountryId).ToArray(),
                    updated_at = l.UpdatedAt
                })
                .ToListAsync();

            return Ok(new { data = items, meta = new { limit, offset, count = items.Count } });
        }
    }
}
