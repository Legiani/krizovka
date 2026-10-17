using System.Text.Json;

namespace Krizovka.Api.Organizations;

public static class LocationResolver
{
    public static List<(double Lon, double Lat)> ParsePolygonCoordinates(string geoJson)
    {
        var coordinates = new List<(double Lon, double Lat)>();
        if (string.IsNullOrWhiteSpace(geoJson)) return coordinates;

        try
        {
            using var doc = JsonDocument.Parse(geoJson);
            var root = doc.RootElement;

            JsonElement coordsElement;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("coordinates", out var coordsProp))
            {
                coordsElement = coordsProp;
            }
            else if (root.ValueKind == JsonValueKind.Array)
            {
                coordsElement = root;
            }
            else
            {
                return coordinates;
            }

            // In GeoJSON Polygon, coordinates is an array of LinearRings: [ [ [lon, lat], ... ] ]
            if (coordsElement.ValueKind == JsonValueKind.Array)
            {
                var outer = coordsElement;
                if (coordsElement.GetArrayLength() > 0 && coordsElement[0].ValueKind == JsonValueKind.Array && coordsElement[0].GetArrayLength() > 0 && coordsElement[0][0].ValueKind == JsonValueKind.Array)
                {
                    outer = coordsElement[0];
                }

                foreach (var point in outer.EnumerateArray())
                {
                    if (point.ValueKind == JsonValueKind.Array && point.GetArrayLength() >= 2)
                    {
                        var lon = point[0].GetDouble();
                        var lat = point[1].GetDouble();
                        coordinates.Add((lon, lat));
                    }
                }
            }
        }
        catch
        {
            // Invalid GeoJSON, return empty
        }

        return coordinates;
    }

    public static bool IsPointInPolygon(double lat, double lon, IReadOnlyList<(double Lon, double Lat)> ring)
    {
        if (ring.Count < 3) return false;
        bool inside = false;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
        {
            var xi = ring[i].Lon;
            var yi = ring[i].Lat;
            var xj = ring[j].Lon;
            var yj = ring[j].Lat;

            bool intersect = ((yi > lat) != (yj > lat))
                && (lon < (xj - xi) * (lat - yi) / (yj - yi) + xi);
            if (intersect)
                inside = !inside;
        }
        return inside;
    }

    public static double DistanceToPolygonMeters(double lat, double lon, IReadOnlyList<(double Lon, double Lat)> ring)
    {
        if (ring.Count < 2) return double.MaxValue;

        var latRad = lat * Math.PI / 180.0;
        var metersPerLat = 111319.5;
        var metersPerLon = 111319.5 * Math.Cos(latRad);

        double minDistance = double.MaxValue;

        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
        {
            var x1 = (ring[j].Lon - lon) * metersPerLon;
            var y1 = (ring[j].Lat - lat) * metersPerLat;
            var x2 = (ring[i].Lon - lon) * metersPerLon;
            var y2 = (ring[i].Lat - lat) * metersPerLat;

            var dx = x2 - x1;
            var dy = y2 - y1;
            var segmentLenSq = dx * dx + dy * dy;

            double distSq;
            if (segmentLenSq < 1e-9)
            {
                distSq = x1 * x1 + y1 * y1;
            }
            else
            {
                // Projection of point (0, 0) onto segment AB:
                // AP = (-x1, -y1), t = (AP . AB) / |AB|^2
                var t = (-x1 * dx + -y1 * dy) / segmentLenSq;
                t = Math.Clamp(t, 0.0, 1.0);
                var projX = x1 + t * dx;
                var projY = y1 + t * dy;
                distSq = projX * projX + projY * projY;
            }

            var dist = Math.Sqrt(distSq);
            if (dist < minDistance)
            {
                minDistance = dist;
            }
        }

        return minDistance;
    }

    public static (bool IsInside, bool IsWithinOffset, double DistanceMeters) EvaluateArea(
        double lat,
        double lon,
        string polygonGeoJson,
        double offsetMeters)
    {
        var coords = ParsePolygonCoordinates(polygonGeoJson);
        if (coords.Count < 3) return (false, false, double.MaxValue);

        var isInside = IsPointInPolygon(lat, lon, coords);
        if (isInside)
        {
            return (true, false, 0.0);
        }

        var distance = DistanceToPolygonMeters(lat, lon, coords);
        var isWithinOffset = distance <= offsetMeters;
        return (false, isWithinOffset, distance);
    }
}
