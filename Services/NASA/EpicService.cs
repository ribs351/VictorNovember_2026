using Microsoft.Extensions.Options;
using VictorNovember.Infrastructure;
using VictorNovember.Infrastructure.Models;
using VictorNovember.Interfaces;
using VictorNovember.Services.NASA.Models;

namespace VictorNovember.Services.NASA;

public sealed class EpicService : IEpicService
{
    private readonly IEpicClient _epicClient;
    private readonly IGeminiService _geminiService;
    public EpicService(
        IEpicClient epicClient, 
        IGeminiService geminiService)
    {
        _epicClient = epicClient;
        _geminiService = geminiService;
    }
    public async Task<EarthImage> GetRandomEarthImageAsync(
        CancellationToken ct = default)
    {
        var images = await _epicClient.GetNaturalAsync(ct);

        if (images.Count == 0)
            throw new InvalidOperationException("NASA EPIC returned no images.");

        var selected = images[Random.Shared.Next(images.Count)];

        var imageUrl = BuildImageUrl(selected);

        var lat = selected.Coords?.Centroid_Coordinates?.Lat;
        var lon = selected.Coords?.Centroid_Coordinates?.Lon;

        return new EarthImage
        {
            Caption = selected.Caption,
            Date = selected.Date,
            ImageUrl = imageUrl,
            Latitude = lat,
            Longitude = lon
        };
    }

    public async Task<string> GenerateCommentaryAsync(EarthImage earthImage, CancellationToken ct = default)
    {
        var commentary = await _geminiService.GenerateVisionCommentaryAsync(
                    earthImage.ImageUrl,
                    earthImage.Latitude,
                    earthImage.Longitude,
                    earthImage.Caption,
                    ct);

        return commentary;
    }

    private static string BuildImageUrl(EpicImage image)
    {
        var date = image.Date;
        return $"https://epic.gsfc.nasa.gov/archive/natural/{date:yyyy}/{date:MM}/{date:dd}/png/{image.Image}.png";
    }
}
