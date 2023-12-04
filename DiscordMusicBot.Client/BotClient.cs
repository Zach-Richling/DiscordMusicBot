using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordMusicBot.Core.Modules;
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
        private MusicModule _musicModule;

        //Expose some information from the discord client for the UI to display.
        public string LoginStatus { get { return _discordClient.LoginState.ToString(); } }
        public string ConnectionStatus { get { return _discordClient.ConnectionState.ToString(); } }

        public event Func<Task> LoggedIn 
        { 
            add { _discordClient.LoggedIn += value; } 
            remove { _discordClient.LoggedIn -= value; } 
        }

        public event Func<Task> LoggedOut
        {
            add { _discordClient.LoggedOut += value; }
            remove { _discordClient.LoggedOut -= value; }
        }

        public event Func<Task> Connected
        {
            add { _discordClient.Connected += value; }
            remove { _discordClient.Connected -= value; }
        }

        public event Func<Exception, Task> Disconnected
        {
            add { _discordClient.Disconnected += value; }
            remove { _discordClient.Disconnected -= value; }
        }

        public BotClient(IConfiguration appConfig, IServiceProvider serviceProvider, DiscordSocketConfig discordConfig, InteractionServiceConfig interactionConfig, MusicModule musicModule)
        {
            _config = appConfig;
            _discordClient = new DiscordSocketClient(discordConfig);
            _serviceProvider = serviceProvider;
            _interactionConfig = interactionConfig;
            _interactionService = new InteractionService(_discordClient, _interactionConfig);
            _musicModule = musicModule;
        }

        public async Task LoginAndStart()
        {
            //Login using BotToken from appsettings.json and start bot
            _discordClient.Log += LogAsync;
            await _discordClient.LoginAsync(TokenType.Bot, _config["BotToken"]);
            await _discordClient.StartAsync();
            _discordClient.Ready += Ready;
            //TODO: Forward Connected and Disconnected events
        }

        public async Task LogoutAndStop()
        {
            await _musicModule.RemoveAllHandlers();
            await _discordClient.LogoutAsync();
            await _discordClient.StopAsync();
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

            _discordClient.UserVoiceStateUpdated += _musicModule.UserVoiceEvent;

            await _discordClient.SetCustomStatusAsync($"Serving {_discordClient.Guilds.Count} servers");
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