using System.Text.Json;
using Krizovka.Api.Organizations;
using Xunit;

namespace Krizovka.Api.Tests;

public class LocationResolverTests
{
    // A square polygon around Prague center (approx 50.080 - 50.090 N, 14.410 - 14.430 E)
    private readonly string _testPolygonGeoJson = JsonSerializer.Serialize(new
    {
        type = "Polygon",
        coordinates = new[]
        {
            new[]
            {
                new[] { 14.4100, 50.0800 },
                new[] { 14.4300, 50.0800 },
                new[] { 14.4300, 50.0900 },
                new[] { 14.4100, 50.0900 },
                new[] { 14.4100, 50.0800 }
            }
        }
    });

    [Fact]
    public void Point_Inside_Core_Polygon_Is_Detected_Correctly()
    {
        // 50.085, 14.420 is right in the middle
        var (isInside, isWithinOffset, dist) = LocationResolver.EvaluateArea(
            lat: 50.0850,
            lon: 14.4200,
            polygonGeoJson: _testPolygonGeoJson,
            offsetMeters: 50.0
        );

        Assert.True(isInside);
        Assert.False(isWithinOffset);
        Assert.Equal(0.0, dist);
    }

    [Fact]
    public void Point_Within_Offset_Buffer_Is_Detected_Correctly()
    {
        // North edge is at 50.0900.
        // Let's place a point slightly north of the edge:
        // 1 deg lat is ~111,319 meters.
        // 0.0003 deg lat is approx 33.4 meters north of 50.0900 -> 50.0903.
        var (isInside, isWithinOffset, dist) = LocationResolver.EvaluateArea(
            lat: 50.0903,
            lon: 14.4200,
            polygonGeoJson: _testPolygonGeoJson,
            offsetMeters: 50.0
        );

        Assert.False(isInside);
        Assert.True(isWithinOffset);
        Assert.InRange(dist, 25.0, 40.0);
    }

    [Fact]
    public void Point_Outside_Offset_Buffer_Is_Rejected()
    {
        // 50.1000 is ~1.1 km north of 50.0900.
        var (isInside, isWithinOffset, dist) = LocationResolver.EvaluateArea(
            lat: 50.1000,
            lon: 14.4200,
            polygonGeoJson: _testPolygonGeoJson,
            offsetMeters: 50.0
        );

        Assert.False(isInside);
        Assert.False(isWithinOffset);
        Assert.True(dist > 500.0);
    }
}
