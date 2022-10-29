using Discord.WebSocket;
using DiscordMusicBot.Client;
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
            var configBuilder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            var appconfig = configBuilder.Build();
            var services = new ServiceCollection();

            var discordConfig = new DiscordSocketConfig()
            {
                //TODO: Add config if necessary
            };

            services.AddSingleton<IConfiguration>(appconfig);
            services.AddSingleton(discordConfig);
            services.AddSingleton<BotClient>();

            return services.BuildServiceProvider();
        }

        private async Task RunAsync(string[] args)
        {
            _serviceProvider.GetRequiredService<BotClient>();
            await Task.Delay(-1);
        }
    }
}