using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Client.InteractionHandlers
{
    public class QueueHandler : InteractionModuleBase
    {
        [SlashCommand("ping", "Ping Pong!")]
        public async Task PingAsync()
        {
            await RespondAsync("Pong!");
        }
    }
}
