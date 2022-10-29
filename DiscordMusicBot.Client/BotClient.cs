using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace DiscordMusicBot.Client
{
    public class BotClient
    {
        private readonly IConfiguration _config;
        private readonly DiscordSocketClient _discordClient;
        public BotClient(IConfiguration appConfig, DiscordSocketConfig discordConfig)
        {
            _config = appConfig;
            _discordClient = new DiscordSocketClient(discordConfig);
            LoginAndStart();
        }

        private async void LoginAndStart()
        {
            await _discordClient.LoginAsync(TokenType.Bot, _config["BotToken"]);
            await _discordClient.StartAsync();
            _discordClient.Ready += Ready;
            Console.WriteLine("Running!");
        }

        private async Task Ready()
        {
            var command = new SlashCommandBuilder();

            command.WithName("ping");
            command.WithDescription("Ping Pong!");

            await _discordClient.CreateGlobalApplicationCommandAsync(command.Build());
        }

    }
}