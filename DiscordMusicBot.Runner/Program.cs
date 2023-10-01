using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordMusicBot.Client;
using DiscordMusicBot.Core.Data;
using DiscordMusicBot.Core.Modules;
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
                UseInteractionSnowflakeDate = false,
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            };

            var interactionConfig = new InteractionServiceConfig()
            {
                AutoServiceScopes = true
            };

            services.AddSingleton<IConfiguration>(appconfig);
            services.AddSingleton(discordConfig);
            services.AddSingleton<BotClient>();
            services.AddSingleton(interactionConfig);
            services.AddSingleton<MediaDownloader>();
            services.AddSingleton<MusicModule>();
            services.AddSingleton<MovieModule>();

            return services.BuildServiceProvider();
        }

        private async Task RunAsync(string[] args)
        {
            _serviceProvider.GetRequiredService<BotClient>();
            await Task.Delay(-1);
        }
    }
}