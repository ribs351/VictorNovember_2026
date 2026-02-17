using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VictorNovember.Data;
using VictorNovember.Infrastructure;
using VictorNovember.Interfaces;
using VictorNovember.Services;
using VictorNovember.Services.Memorial;
using VictorNovember.Services.NASA;
using VictorNovember.Services.Welcome;
using VictorNovember.Utils;

namespace VictorNovember;

public sealed class Program
{
    public static async Task Main(string[] args)
    {

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddDbContextFactory<NovemberContext>(options =>
                {
                    options.UseSqlServer(
                        context.Configuration.GetConnectionString("NovemberDb"));
                });
                services.AddMemoryCache();
                services.AddTransient<IGeminiService, GeminiService>();
                services.AddScoped<ServerTrackingService>();
                services.AddHostedService<DiscordBotService>();
                services.AddHttpClient("welcome-images", client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                });
                services.Configure<NasaOptions>(context.Configuration.GetSection("Nasa"));
                services.AddHttpClient<INasaClient, NasaClient>(client =>
                {
                    client.BaseAddress = new Uri("https://api.nasa.gov/");
                });
                services.AddHttpClient<IEpicClient, EpicClient>(client =>
                {
                    client.BaseAddress = new Uri("https://epic.gsfc.nasa.gov/api/");
                });
                services.AddTransient<IEpicService, EpicService>();
                services.AddTransient<WelcomeConfigurationService>();
                services.AddTransient<WelcomeImageRenderer>();
                //services.AddTransient<IImageDownloader, ImageDownloader>();
                services.AddTransient<IApodService, ApodService>();
                services.AddSingleton<IPromptProviderService, FilePromptProviderService>();
                services.AddSingleton<DiscordClientProvider>();
                // Memorial Sigil Service, lest we forget...
                services.AddHangfire(config => config
                        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                        .UseSimpleAssemblyNameTypeSerializer()
                        .UseRecommendedSerializerSettings()
                        .UseSqlServerStorage(context.Configuration.GetConnectionString("NovemberDb")));
                services.AddHangfireServer();
                services.AddTransient<IMemorialService, MemorialService>();
            })
            .Build();

        await host.RunAsync();
    }
}
