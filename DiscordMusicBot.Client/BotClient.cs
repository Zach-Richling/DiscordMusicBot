using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace DiscordMusicBot.Client
{
    public class BotClient
    {
        private readonly IConfiguration _config;
        private readonly DiscordSocketClient _discordClient;
        private readonly IServiceProvider _serviceProvider;
        private InteractionService _interactionService;
        public BotClient(IConfiguration appConfig, IServiceProvider serviceProvider, DiscordSocketConfig discordConfig)
        {
            _config = appConfig;
            _discordClient = new DiscordSocketClient(discordConfig);
            _serviceProvider = serviceProvider;
            LoginAndStart();
        }

        private async void LoginAndStart()
        {
            //Login using BotToken from appsettings.json and start bot
            await _discordClient.LoginAsync(TokenType.Bot, _config["BotToken"]);
            await _discordClient.StartAsync();
            _discordClient.Ready += Ready;
        }

        private async Task Ready()
        {
            //Set up slash commands using interaction service
            _interactionService = new InteractionService(_discordClient);
            //Add all modules from DiscordMusicBot.Client
            await _interactionService.AddModulesAsync(Assembly.GetExecutingAssembly(), _serviceProvider);
            //Register commands to all guilds
            await _interactionService.RegisterCommandsGloballyAsync();

            //Route Slash Commands to their respective methods when called
            _discordClient.InteractionCreated += async interaction =>
            {
                var ctx = new SocketInteractionContext(_discordClient, interaction);
                await _interactionService.ExecuteCommandAsync(ctx, _serviceProvider);
            };

        }
    }
}