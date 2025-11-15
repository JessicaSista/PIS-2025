using System.Text.Json;
using System.Text.Json.Serialization;

namespace OmniMonitor.Shared.Dtos
{
    public class Geometry
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }

    public class Circle : Geometry
    {
        [JsonPropertyName("center")]
        public double[] Center { get; set; } = new double[2]; // [lng, lat]

        [JsonPropertyName("radius")]
        public double Radius { get; set; }

        public bool ContainsPoint(double lat, double lng)
        {
            // Calcular distancia entre el punto y el centro del círculo
            var distance = CalculateDistance(lat, lng, Center[1], Center[0]);
            return distance <= Radius;
        }

        private double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
        {
            const double R = 6371000; // Radio de la Tierra en metros
            var dLat = ToRadians(lat2 - lat1);
            var dLng = ToRadians(lng2 - lng1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }
    }

    public class Polygon : Geometry
    {
        [JsonPropertyName("coordinates")]
        public double[][][] Coordinates { get; set; } = new double[0][][];

        public bool ContainsPoint(double lat, double lng)
        {
            if (Coordinates.Length == 0 || Coordinates[0].Length == 0)
                return false;

            var polygon = Coordinates[0]; // Tomar el primer anillo del polígono
            var x = lng;
            var y = lat;
            var inside = false;

            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                var xi = polygon[i][0];
                var yi = polygon[i][1];
                var xj = polygon[j][0];
                var yj = polygon[j][1];

                if (((yi > y) != (yj > y)) && (x < (xj - xi) * (y - yi) / (yj - yi) + xi))
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }

    public static class GeometryHelper
    {
        public static bool IsPointInZone(Location eventLocation, List<string> zoneAreas)
        {
            if (eventLocation == null || zoneAreas == null || !zoneAreas.Any())
                return false;

            foreach (var areaJson in zoneAreas)
            {
                try
                {
                    var geometry = JsonSerializer.Deserialize<Geometry>(areaJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (geometry == null) continue;

                    bool isInside = geometry.Type.ToLower() switch
                    {
                        "circle" => IsPointInCircle(eventLocation, areaJson),
                        "polygon" => IsPointInPolygon(eventLocation, areaJson),
                        _ => false
                    };

                    if (isInside)
                        return true;
                }
                catch (JsonException)
                {
                    continue;
                }
            }

            return false;
        }

        private static bool IsPointInCircle(Location eventLocation, string circleJson)
        {
            try
            {
                var circle = JsonSerializer.Deserialize<Circle>(circleJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return circle?.ContainsPoint(eventLocation.Lat, eventLocation.Lng) ?? false;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsPointInPolygon(Location eventLocation, string polygonJson)
        {
            try
            {
                var polygon = JsonSerializer.Deserialize<Polygon>(polygonJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return polygon?.ContainsPoint(eventLocation.Lat, eventLocation.Lng) ?? false;
            }
            catch
            {
                return false;
            }
        }
    }
}
