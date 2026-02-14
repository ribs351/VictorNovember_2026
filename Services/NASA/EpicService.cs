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
        IOptions<NasaOptions> options, 
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

        return new EarthImage
        {
            Caption = selected.Caption,
            Date = selected.Date,
            ImageUrl = imageUrl
        };
    }

    public async Task<string> GenerateCommentary(EarthImage earthImage, CancellationToken ct = default)
    {
        var commentary = await _geminiService.GenerateVisionCommentaryAsync(
                    earthImage.ImageUrl,
                    earthImage.Caption,
                    ct);

        return commentary;
    }

    private string BuildImageUrl(EpicImage image)
    {
        var date = image.Date;

        var year = date.ToString("yyyy");
        var month = date.ToString("MM");
        var day = date.ToString("dd");

        return $"https://epic.gsfc.nasa.gov/archive/natural/{year}/{month}/{day}/png/{image.Image}.png";
    }
}
