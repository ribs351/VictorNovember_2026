using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VictorNovember.ApplicationCommands;
using VictorNovember.BasicCommands;
using VictorNovember.Data;
using VictorNovember.Services;
using VictorNovember.Utils;

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
            })
            .Build();

        await host.RunAsync();
    }
}
