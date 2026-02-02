using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using poc_locations_service.Data;
using poc_locations_service.Dtos;

namespace poc_locations_service.Controllers
{
    [ApiController]
    public class CountriesController : Controller
    {
        private readonly AppDbContext _db;
        public CountriesController(AppDbContext db) => _db = db;

        // Public list (ID discovery)
        [HttpGet("v1/countries")]
        public async Task<IActionResult> List([FromQuery] string status = "ACTIVE")
        {
            var q = _db.Countries.AsNoTracking();
            q = q.Where(c => c.Status == status.ToUpperInvariant());
            var items = await q.OrderBy(x => x.CountryId).ToListAsync();
            return Ok(new { data = items });
        }

        // Admin create
        [HttpPost("v1/admin/countries")]
        public async Task<IActionResult> Create([FromBody] CreateCountryRequest req)
        {
            var exists = await _db.Countries.AnyAsync(x => x.CountryId == req.CountryId);
            if (exists) return Conflict(new { error = "country_id already exists" });

            var c = new Country
            {
                CountryId = req.CountryId,
                Name = req.Name,
                Status = RecordStatuses.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Countries.Add(c);
            await _db.SaveChangesAsync();
            return Created($"/v1/admin/countries/{c.CountryId}", c);
        }

        [HttpGet("v1/admin/countries/{countryId}")]
        public async Task<IActionResult> Get(string countryId)
        {
            var c = await _db.Countries.AsNoTracking().FirstOrDefaultAsync(x => x.CountryId == countryId);
            return c is null ? NotFound() : Ok(c);
        }

        [HttpPatch("v1/admin/countries/{countryId}")]
        public async Task<IActionResult> Patch(string countryId, [FromBody] PatchCountryRequest req)
        {
            var c = await _db.Countries.FirstOrDefaultAsync(x => x.CountryId == countryId);
            if (c is null) return NotFound();

            if (!string.IsNullOrWhiteSpace(req.Name)) c.Name = req.Name;
            if (!string.IsNullOrWhiteSpace(req.Status)) c.Status = req.Status.Trim().ToUpperInvariant();
            c.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(c);
        }
    }
}
