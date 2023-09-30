using Discord;
using Discord.Commands;
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
        private readonly InteractionServiceConfig _interactionConfig;
        private InteractionService _interactionService;
        public BotClient(IConfiguration appConfig, IServiceProvider serviceProvider, DiscordSocketConfig discordConfig, InteractionServiceConfig interactionConfig)
        {
            _config = appConfig;
            _discordClient = new DiscordSocketClient(discordConfig);
            _serviceProvider = serviceProvider;
            _interactionConfig = interactionConfig;
            _interactionService = new InteractionService(_discordClient, _interactionConfig);
            LoginAndStart();
        }

        private async void LoginAndStart()
        {
            //Login using BotToken from appsettings.json and start bot
            Directory.CreateDirectory(_config["SongDir"]!);
            _discordClient.Log += LogAsync;
            await _discordClient.LoginAsync(TokenType.Bot, _config["BotToken"]);
            await _discordClient.StartAsync();
            _discordClient.Ready += Ready;
        }

        private async Task Ready()
        {
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

        private Task LogAsync(LogMessage message)
        {
            if (message.Exception is CommandException cmdException)
            {
                Console.WriteLine($"[Command/{message.Severity}] {cmdException.Command.Aliases.First()}"
                    + $" failed to execute in {cmdException.Context.Channel}.");
                Console.WriteLine(cmdException);
            }
            else
                Console.WriteLine($"[General/{message.Severity}] {message}");

            return Task.CompletedTask;
        }
    }
}