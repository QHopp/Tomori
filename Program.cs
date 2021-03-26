using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tomori.Services;

namespace Tomori
{
    public class Program
    {
        public static async Task Main()
        {
            var discord = new DiscordClient(new DiscordConfiguration()
            {
                AlwaysCacheMembers = false,
                MessageCacheSize = 0,
                MinimumLogLevel = LogLevel.Information,
                Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN"),
            });

            var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
            {
                DmHelp = true,
                EnableMentionPrefix = false,
                IgnoreExtraArguments = true,
                StringPrefixes = new[] {"&"},
                Services = GetServices(),
            });
            commands.RegisterCommands(Assembly.GetExecutingAssembly());
            await discord.ConnectAsync();
            await Task.Delay(-1);
        }

        private static IServiceProvider GetServices()
        {
            return new ServiceCollection()
                .AddSingleton<RssService>()
                .AddSingleton<HttpClient>()
                .AddSingleton<Random>()
                .BuildServiceProvider();
        }
    }
}