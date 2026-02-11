using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VictorNovember.Data;
using VictorNovember.Services;

namespace VictorNovember;

public sealed class Program
{
    public static async Task Main(string[] args)
    {

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddDbContext<NovemberContext>(options =>
                {
                    options.UseSqlServer(
                        context.Configuration.GetConnectionString("NovemberDb"));
                });

                services.AddMemoryCache();
                services.AddTransient<GoogleGeminiService>();
                services.AddScoped<ServerBootstrapService>();
                services.AddHostedService<DiscordBotService>();
                services.AddHttpClient("welcome-images", client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                });
                services.AddTransient<WelcomeImageService>();
            })
            .Build();

        await host.RunAsync();
    }
}
