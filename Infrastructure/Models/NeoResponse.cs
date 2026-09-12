using System.Text.Json.Serialization;

namespace VictorNovember.Infrastructure.Models;

public sealed class NeoFeedResponse
{
    [JsonPropertyName("element_count")]
    public int ElementCount { get; set; }
    [JsonPropertyName("near_earth_objects")]
    public Dictionary<string, List<NearEarthObject>> NearEarthObjectsByDate { get; set; } = new();
    [JsonIgnore]
    public IEnumerable<NearEarthObject> AllAsteroids =>
        NearEarthObjectsByDate.Values.SelectMany(list => list);
}

public sealed class NearEarthObject
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("nasa_jpl_url")]
    public string NasaJplUrl { get; set; } = string.Empty;

    [JsonPropertyName("absolute_magnitude_h")]
    public double AbsoluteMagnitudeH { get; set; }

    [JsonPropertyName("estimated_diameter")]
    public EstimatedDiameter EstimatedDiameter { get; set; } = new();

    [JsonPropertyName("is_potentially_hazardous_asteroid")]
    public bool IsPotentiallyHazardous { get; set; }

    [JsonPropertyName("close_approach_data")]
    public List<CloseApproachData> CloseApproachData { get; set; } = new();
    [JsonIgnore]
    public CloseApproachData? PrimaryApproach => CloseApproachData.FirstOrDefault();
}

public sealed class EstimatedDiameter
{
    [JsonPropertyName("meters")]
    public DiameterRange Meters { get; set; } = new();
}

public sealed class DiameterRange
{
    [JsonPropertyName("estimated_diameter_min")]
    public double Min { get; set; }

    [JsonPropertyName("estimated_diameter_max")]
    public double Max { get; set; }
}

public sealed class CloseApproachData
{
    [JsonPropertyName("close_approach_date")]
    public string CloseApproachDate { get; set; } = string.Empty;

    [JsonPropertyName("relative_velocity")]
    public RelativeVelocity RelativeVelocity { get; set; } = new();

    [JsonPropertyName("miss_distance")]
    public MissDistance MissDistance { get; set; } = new();

    [JsonPropertyName("orbiting_body")]
    public string OrbitingBody { get; set; } = string.Empty;
}

public sealed class RelativeVelocity
{
    [JsonPropertyName("kilometers_per_hour")]
    public string KilometersPerHour { get; set; } = string.Empty;

    [JsonPropertyName("kilometers_per_second")]
    public string KilometersPerSecond { get; set; } = string.Empty;
}

public sealed class MissDistance
{
    [JsonPropertyName("kilometers")]
    public string Kilometers { get; set; } = string.Empty;

    [JsonPropertyName("lunar")]
    public string Lunar { get; set; } = string.Empty;
}