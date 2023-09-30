using Discord;
using Discord.Interactions;
using DiscordMusicBot.Core.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Client.InteractionHandlers
{
    public class MovieHandler : InteractionModuleBase
    {
        private readonly MovieModule _movieModule;
        public MovieHandler(MovieModule movieModule)
        {
            _movieModule = movieModule;
        }

        [SlashCommand("pickmovie", "Pick a movie from movie-time", runMode: RunMode.Async)]
        public async Task PickMovieAsync()
        {
            await DeferAsync();
            var builder = InitializeEmbedBuilder();
            builder.WithDescription($"{await _movieModule.SelectMovie(Context)}");
            await FollowupAsync(embed: builder.Build());
        }

        private EmbedBuilder InitializeEmbedBuilder()
        {
            return new EmbedBuilder().WithColor(Color.Orange);
        }
    }
}
