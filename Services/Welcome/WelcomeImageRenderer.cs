using DSharpPlus.Entities;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace VictorNovember.Services.Welcome;

public sealed class WelcomeImageRenderer
{
    private readonly IHttpClientFactory _httpClientFactory;
    private const string DefaultBackgroundUrl = "https://images.unsplash.com/photo-1461511669078-d46bf351cd6e?w=1100&h=450&fit=crop";
    public WelcomeImageRenderer(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Stream> CreateWelcomeImageAsync(
        DiscordMember member,
        string? backgroundUrl = null,
        CancellationToken cancellationToken = default)
    {
        var avatarUrl = member.AvatarUrl ?? member.DefaultAvatarUrl;
        using var avatar = await FetchImageAsync(avatarUrl, cancellationToken);
        using var background = await FetchImageAsync(backgroundUrl ?? DefaultBackgroundUrl, cancellationToken);

        var banner = CreateBanner(background, avatar, member);

        var outputStream = new MemoryStream();
        await banner.SaveAsync(outputStream, new PngEncoder(), cancellationToken);
        outputStream.Position = 0;

        return outputStream;
    }

    private Image CreateBanner(Image background, Image avatar, DiscordMember member)
    {
        const int bannerWidth = 1100;
        const int bannerHeight = 450;

        // Resize and crop background to banner size
        background.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(bannerWidth, bannerHeight),
            Mode = ResizeMode.Crop
        }));

        // Create circular avatar (220x220)
        const int avatarSize = 220;
        using var circularAvatar = CreateCircularImage(avatar, avatarSize);

        // Composite avatar onto background (centered horizontally, offset vertically)
        var avatarX = bannerWidth / 2 - avatarSize / 2;
        var avatarY = bannerHeight / 2 - 155;

        background.Mutate(ctx =>
        {
            ctx.DrawImage(circularAvatar, new Point(avatarX, avatarY), 1f);
        });

        circularAvatar.Dispose();

        // Draw text
        DrawTextOnBanner(
            background,
            $"{member.Username} joined the server",
            $"Member #{member.Guild.MemberCount}"
        );

        return background;
    }

    private static Image CreateCircularImage(Image source, int size)
    {
        // Resize source to square
        source.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(size, size),
            Mode = ResizeMode.Crop
        }));

        // Create circular mask
        var output = new Image<Rgba32>(size, size);

        output.Mutate(ctx =>
        {
            // Create circle path
            var circle = new EllipsePolygon(size / 2f, size / 2f, size / 2f);

            // Draw the source image clipped to the circle
            ctx.Fill(Color.Transparent);
            ctx.SetGraphicsOptions(new GraphicsOptions { AlphaCompositionMode = PixelAlphaCompositionMode.SrcOver });

            // Use the circle as a clipping mask
            ctx.Fill(
                new ImageBrush(source),
                circle
            );
        });

        return output;
    }

    private static void DrawTextOnBanner(Image banner, string header, string subheader)
    {
        const int bannerWidth = 1100;
        const int bannerHeight = 450;

        // Load font
        FontFamily fontFamily;

        if (!SystemFonts.TryGet("Noto Sans", out fontFamily))
        {
            fontFamily = SystemFonts.Families.First();
        }

        var headerFont = fontFamily.CreateFont(40, FontStyle.Bold);
        var subheaderFont = fontFamily.CreateFont(28, FontStyle.Regular);

        var headerX = bannerWidth / 2f;
        var headerY = bannerHeight / 2f + 95;
        var subheaderX = bannerWidth / 2f;
        var subheaderY = bannerHeight / 2f + 140;

        banner.Mutate(ctx =>
        {
            var headerOptions = new RichTextOptions(headerFont)
            {
                Origin = new PointF(headerX, headerY),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var subOptions = new RichTextOptions(subheaderFont)
            {
                Origin = new PointF(subheaderX, subheaderY),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var softStrokeHeader = Pens.Solid(Color.FromRgba(0, 0, 0, 180), 2f);
            //var softStrokeSub = Pens.Solid(Color.FromRgba(0, 0, 0, 150), 0.75f);

            // Fill colors
            var headerBrush = Brushes.Solid(Color.White);
            var subBrush = Brushes.Solid(Color.FromRgb(210, 210, 210)); // softer grey

            ctx.DrawText(headerOptions, header, headerBrush, softStrokeHeader);
            ctx.DrawText(subOptions, subheader, subBrush, null);
        });
    }

    private async Task<Image> FetchImageAsync(string url, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient("welcome-images");

        try
        {
            var response = await httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // Fallback to default background
                response = await httpClient.GetAsync(DefaultBackgroundUrl, cancellationToken);
            }

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await Image.LoadAsync(stream, cancellationToken);
        }
        catch
        {
            // If everything fails, return a solid color image
            var fallback = new Image<Rgba32>(1100, 450);
            fallback.Mutate(x => x.Fill(Color.DarkSlateGray));
            return fallback;
        }
    }
}
