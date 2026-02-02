using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using poc_locations_service.Data;
using poc_locations_service.Dtos;

namespace poc_locations_service.Controllers
{

    [ApiController]

    public class MapsController : Controller
    {

        private readonly AppDbContext _db;
        public MapsController(AppDbContext db) => _db = db;

        // Public list (ID discovery)
        [HttpGet("v1/maps")]
        public async Task<IActionResult> List([FromQuery] string status = "ACTIVE")
        {
            var s = status.ToUpperInvariant();
            var items = await _db.Maps.AsNoTracking()
                .Where(m => m.Status == s)
                .OrderBy(m => m.MapId)
                .ToListAsync();

            return Ok(new { data = items });
        }

        [HttpPost("v1/admin/maps")]
        public async Task<IActionResult> Create([FromBody] CreateMapRequest req)
        {
            var exists = await _db.Maps.AnyAsync(x => x.MapId == req.MapId);
            if (exists) return Conflict(new { error = "map_id already exists" });

            var m = new MapEntity
            {
                MapId = req.MapId,
                Name = req.Name,
                Description = req.Description,
                Status = RecordStatuses.ACTIVE,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Maps.Add(m);
            await _db.SaveChangesAsync();
            return Created($"/v1/admin/maps/{m.MapId}", m);
        }

        [HttpGet("v1/admin/maps/{mapId}")]
        public async Task<IActionResult> Get(string mapId)
        {
            var m = await _db.Maps.AsNoTracking().FirstOrDefaultAsync(x => x.MapId == mapId);
            return m is null ? NotFound() : Ok(m);
        }

        [HttpPatch("v1/admin/maps/{mapId}")]
        public async Task<IActionResult> Patch(string mapId, [FromBody] PatchMapRequest req)
        {
            var m = await _db.Maps.FirstOrDefaultAsync(x => x.MapId == mapId);
            if (m is null) return NotFound();

            if (!string.IsNullOrWhiteSpace(req.Name)) m.Name = req.Name;
            if (req.Description is not null) m.Description = req.Description;
            if (!string.IsNullOrWhiteSpace(req.Status)) m.Status = req.Status.Trim().ToUpperInvariant();
            m.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(m);
        }
    }
}
