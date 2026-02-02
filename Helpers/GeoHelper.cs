using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace poc_locations_service.Helpers
{
    public static class GeoHelper
    {
        private static readonly GeometryFactory Gf = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        public static Point ToPoint(decimal lat, decimal lng)
            => Gf.CreatePoint(new Coordinate((double)lng, (double)lat));

        // bbox = minLng,minLat,maxLng,maxLat
        public static Polygon BboxToPolygon(double minLng, double minLat, double maxLng, double maxLat)
        {
            var coords = new[]
            {
            new Coordinate(minLng, minLat),
            new Coordinate(maxLng, minLat),
            new Coordinate(maxLng, maxLat),
            new Coordinate(minLng, maxLat),
            new Coordinate(minLng, minLat)
        };
            return Gf.CreatePolygon(coords);
        }

        public static bool TryParseBbox(string bbox, out double minLng, out double minLat, out double maxLng, out double maxLat)
        {
            minLng = minLat = maxLng = maxLat = 0;
            var parts = bbox.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4) return false;

            return double.TryParse(parts[0], out minLng)
                && double.TryParse(parts[1], out minLat)
                && double.TryParse(parts[2], out maxLng)
                && double.TryParse(parts[3], out maxLat)
                && minLng <= maxLng
                && minLat <= maxLat;
        }

        public static string NormalizeMode(string? mode)
            => string.IsNullOrWhiteSpace(mode) ? "MERGE" : mode.Trim().ToUpperInvariant();
    }

}
