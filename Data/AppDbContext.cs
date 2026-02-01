using Microsoft.EntityFrameworkCore;

namespace poc_locations_service.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Country> Countries => Set<Country>();
        public DbSet<MapEntity> Maps => Set<MapEntity>();
        public DbSet<Location> Locations => Set<Location>();
        public DbSet<LocationCountry> LocationCountries => Set<LocationCountry>();
        public DbSet<LocationMap> LocationMaps => Set<LocationMap>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // --- countries ---
            modelBuilder.Entity<Country>(e =>
            {
                e.ToTable("countries", "dbo");
                e.HasKey(x => x.CountryId);
                e.Property(x => x.CountryId).HasColumnName("country_id");
                e.Property(x => x.Name).HasColumnName("name");
                e.Property(x => x.Status).HasColumnName("status");
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            });

            // --- maps ---
            modelBuilder.Entity<MapEntity>(e =>
            {
                e.ToTable("maps", "dbo");
                e.HasKey(x => x.MapId);
                e.Property(x => x.MapId).HasColumnName("map_id");
                e.Property(x => x.Name).HasColumnName("name");
                e.Property(x => x.Description).HasColumnName("description");
                e.Property(x => x.Status).HasColumnName("status");
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            });

            // --- locations ---
            modelBuilder.Entity<Location>(e =>
            {
                e.ToTable("locations", "dbo");
                e.HasKey(x => x.LocationId);
                e.Property(x => x.LocationId).HasColumnName("location_id");
                e.Property(x => x.ExternalReference).HasColumnName("external_reference");
                e.Property(x => x.Name).HasColumnName("name");
                e.Property(x => x.AddressLine1).HasColumnName("address_line1");
                e.Property(x => x.AddressLine2).HasColumnName("address_line2");
                e.Property(x => x.City).HasColumnName("city");
                e.Property(x => x.Region).HasColumnName("region");
                e.Property(x => x.PostalCode).HasColumnName("postal_code");

                e.Property(x => x.Latitude).HasColumnName("latitude");
                e.Property(x => x.Longitude).HasColumnName("longitude");

                e.Property(x => x.Geom).HasColumnName("geom").HasColumnType("geography");

                e.Property(x => x.Phone).HasColumnName("phone");
                e.Property(x => x.Email).HasColumnName("email");
                e.Property(x => x.WebsiteUrl).HasColumnName("website_url");
                e.Property(x => x.HoursText).HasColumnName("hours_text");

                e.Property(x => x.Status).HasColumnName("status");
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
                e.Property(x => x.CreatedBy).HasColumnName("created_by");
                e.Property(x => x.UpdatedBy).HasColumnName("updated_by");
            });

            // --- location_country ---
            modelBuilder.Entity<LocationCountry>(e =>
            {
                e.ToTable("location_country", "dbo");
                e.HasKey(x => new { x.LocationId, x.CountryId });

                e.Property(x => x.LocationId).HasColumnName("location_id");
                e.Property(x => x.CountryId).HasColumnName("country_id");
                e.Property(x => x.Status).HasColumnName("status");
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.Property(x => x.UpdatedAt).HasColumnName("updated_at");

                e.HasOne(x => x.Location)
                    .WithMany(l => l.LocationCountries)
                    .HasForeignKey(x => x.LocationId);

                e.HasOne(x => x.Country)
                    .WithMany(c => c.LocationCountries)
                    .HasForeignKey(x => x.CountryId);
            });

            // --- location_map ---
            modelBuilder.Entity<LocationMap>(e =>
            {
                e.ToTable("location_map", "dbo");
                e.HasKey(x => new { x.LocationId, x.MapId });

                e.Property(x => x.LocationId).HasColumnName("location_id");
                e.Property(x => x.MapId).HasColumnName("map_id");
                e.Property(x => x.Status).HasColumnName("status");
                e.Property(x => x.CreatedAt).HasColumnName("created_at");
                e.Property(x => x.UpdatedAt).HasColumnName("updated_at");

                e.HasOne(x => x.Location)
                    .WithMany(l => l.LocationMaps)
                    .HasForeignKey(x => x.LocationId);

                e.HasOne(x => x.Map)
                    .WithMany(m => m.LocationMaps)
                    .HasForeignKey(x => x.MapId);
            });
        }

    }
}

