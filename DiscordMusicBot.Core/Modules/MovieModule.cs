using Discord;
using DiscordMusicBot.Core.Data;
using DiscordMusicBot.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Core.Modules
{
    public class MovieModule
    {
        public async Task<string> SelectMovie(IInteractionContext context)
        {
            var channels = await context.Guild.GetChannelsAsync();
            var channelId = channels.Where(x => x.Name == "movie-time").Select(x => x.Id).FirstOrDefault();

            if (channelId == 0)
            {
                return "";
            }

            var channel = (IMessageChannel)(await context.Guild.GetChannelAsync(channelId));
            var messages = await channel.GetMessagesAsync(500).FlattenAsync();
            var filteredMessages = messages.Where(x => !x.Reactions.Select(y => y.Key.Name).Contains("white_check_mark")).ToList();

            var random = new Random();
            var selectedMessage = filteredMessages[random.Next(filteredMessages.Count)];
            return selectedMessage.Content;
        }
    }
}
