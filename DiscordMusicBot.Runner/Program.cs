using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordMusicBot.Client;
using DiscordMusicBot.Core.Data;
using DiscordMusicBot.Core.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DiscordMusicBot.Runner
{
    public class Program
    {
        private readonly IServiceProvider _serviceProvider;
        public Program()
        {
            _serviceProvider = CreateProvider();
        }

        static void Main(string[] args)
        {
            new Program().RunAsync().GetAwaiter().GetResult();
            Task.Delay(-1).GetAwaiter().GetResult();
        }

        private static IServiceProvider CreateProvider()
        {
            var appconfig = new ConfigurationBuilder().AddJsonFile(Path.Combine("resources", "appsettings.json")).Build();
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
            services.AddSingleton<BaseFunctions>();
            services.AddSingleton<AiImageModule>();
            services.AddSingleton<FalAIClient>();

            services.AddHttpClient();

            return services.BuildServiceProvider();
        }

        public async Task RunAsync()
        {
            var botClient = _serviceProvider.GetRequiredService<BotClient>();
            await botClient.LoginAndStart();
        }

        public async Task StopAsync()
        {
            var botClient = _serviceProvider.GetRequiredService<BotClient>();
            await botClient.LogoutAndStop();
        }

        public BotClient GetBotClient()
        {
            return _serviceProvider.GetRequiredService<BotClient>();
        }
    }
}