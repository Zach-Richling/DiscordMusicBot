using Discord;
using DiscordMusicBot.Core.Data.Youtube;
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
        private ConcurrentDictionary<ulong, GuildMovieHandler> _guildHandlers = new();

        private GuildMovieHandler GetOrAddGuild(IInteractionContext context)
        {
            return _guildHandlers.GetOrAdd(context.Guild.Id, new GuildMovieHandler(context));
        }

        public async Task<string> SelectMovie(IInteractionContext context)
        {
            return await GetOrAddGuild(context).SelectMovie();
        }

        private class GuildMovieHandler
        {
            private IInteractionContext _context;

            public GuildMovieHandler(IInteractionContext context)
            {
                _context = context;
            }

            public async Task<string> SelectMovie()
            {
                var channels = await _context.Guild.GetChannelsAsync();
                var channelId = channels.Where(x => x.Name == "movie-time").Select(x => x.Id).FirstOrDefault();

                if (channelId == 0)
                {
                    return "";
                }

                var channel = (IMessageChannel)(await _context.Guild.GetChannelAsync(channelId));
                var messages = await channel.GetMessagesAsync(500).FlattenAsync();
                var filteredMessages = messages.Where(x => !x.Reactions.Select(y => y.Key.Name).Contains("white_check_mark")).ToList();

                var random = new Random();
                var selectedMessage = filteredMessages[random.Next(filteredMessages.Count)];
                return selectedMessage.Content;
            }
        }
    }
}
