using Discord.Interactions;
using Discord.WebSocket;
using DiscordMusicBot.Client;
using DiscordMusicBot.Core.Data;
using DiscordMusicBot.Core.Data.Youtube;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordMusicBot.Runner
{
    class Program
    {
        private readonly IServiceProvider _serviceProvider;
        public Program()
        {
            _serviceProvider = CreateProvider();
        }

        static void Main(string[] args) => new Program().RunAsync(args).GetAwaiter().GetResult();

        private static IServiceProvider CreateProvider()
        {
            var appconfig = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            var services = new ServiceCollection();

            var discordConfig = new DiscordSocketConfig()
            {
                LogLevel = Discord.LogSeverity.Info,
                UseInteractionSnowflakeDate = false
            };

            var interactionConfig = new InteractionServiceConfig()
            {
                AutoServiceScopes = true
            };

            var queue = new QueueList();

            services.AddSingleton<IConfiguration>(appconfig);
            services.AddSingleton(discordConfig);
            services.AddSingleton<BotClient>();
            services.AddSingleton(queue);
            services.AddSingleton(interactionConfig);
            services.AddSingleton<YoutubeDownloader>();

            return services.BuildServiceProvider();
        }

        private async Task RunAsync(string[] args)
        {
            _serviceProvider.GetRequiredService<BotClient>();
            await Task.Delay(-1);
        }
    }
}