using NetTopologySuite.Geometries;

namespace poc_locations_service.Data
{
    public static class RecordStatuses
    {
        public const string ACTIVE = "ACTIVE";
        public const string INACTIVE = "INACTIVE";
        public const string DELETED = "DELETED";
    }

    public class Country
    {
        public string CountryId { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Status { get; set; } = RecordStatuses.ACTIVE;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<LocationCountry> LocationCountries { get; set; } = new List<LocationCountry>();
    }

    public class MapEntity
    {
        public string MapId { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public string Status { get; set; } = RecordStatuses.ACTIVE;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<LocationMap> LocationMaps { get; set; } = new List<LocationMap>();
    }

    public class Location
    {
        public Guid LocationId { get; set; }
        public string? ExternalReference { get; set; }

        public string Name { get; set; } = default!;
        public string AddressLine1 { get; set; } = default!;
        public string? AddressLine2 { get; set; }
        public string City { get; set; } = default!;
        public string? Region { get; set; }
        public string? PostalCode { get; set; }

        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }

        public Point Geom { get; set; } = default!; // geography

        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? HoursText { get; set; }

        public string Status { get; set; } = RecordStatuses.ACTIVE;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }

        public ICollection<LocationCountry> LocationCountries { get; set; } = new List<LocationCountry>();
        public ICollection<LocationMap> LocationMaps { get; set; } = new List<LocationMap>();
    }

    public class LocationCountry
    {
        public Guid LocationId { get; set; }
        public Location Location { get; set; } = default!;
        public string CountryId { get; set; } = default!;
        public Country Country { get; set; } = default!;

        public string Status { get; set; } = RecordStatuses.ACTIVE;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class LocationMap
    {
        public Guid LocationId { get; set; }
        public Location Location { get; set; } = default!;
        public string MapId { get; set; } = default!;
        public MapEntity Map { get; set; } = default!;

        public string Status { get; set; } = RecordStatuses.ACTIVE;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

}
