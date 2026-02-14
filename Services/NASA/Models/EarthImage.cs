namespace VictorNovember.Services.NASA.Models;

public sealed class EarthImage
{
    public string Caption { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
