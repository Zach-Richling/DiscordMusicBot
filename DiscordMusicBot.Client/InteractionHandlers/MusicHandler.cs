using Discord;
using Discord.Interactions;
using DiscordMusicBot.Core.Data.Youtube;
using DiscordMusicBot.Core.Models;
using DiscordMusicBot.Core.Modules;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography.X509Certificates;

namespace DiscordMusicBot.Client.InteractionHandlers
{
    public class MusicHandler : InteractionModuleBase
    {
        private readonly MusicModule _musicModule;
        private readonly YoutubeDownloader _youtubeDl;
        private readonly string zeroWidthSpace = '\u200b'.ToString();

        public MusicHandler(MusicModule musicModule, YoutubeDownloader youtubeDl)
        {
            _musicModule = musicModule;
            _youtubeDl = youtubeDl;
        }
        //TODO: Shuffle, 
        [SlashCommand("ping", "Ping Pong!")]
        public async Task PingAsync()
        {
            await RespondAsync("Pong!");
        }

        [SlashCommand("play", "Play a song.", runMode: RunMode.Async)]
        public async Task PlayAsync(string url)
        {
            await PlayInnerAsync(url, false);
        }

        [SlashCommand("playtop", "Play a song at the top of the queue.", runMode: RunMode.Async)]
        public async Task PlayTopAsync(string url)
        {
            await PlayInnerAsync(url, true);
        }

        private async Task PlayInnerAsync(string url, bool top)
        {
            await DeferAsync();
            var builder = new EmbedBuilder();
            builder.WithColor(Color.Orange);

            try
            {
                var songs = await _youtubeDl.ProcessURL(url);
                _musicModule.Play(Context, songs, true);

                if (songs.Count == 1)
                {
                    builder.WithDescription($"{songs[0].Name} added.");
                }
                else
                {
                    builder.WithDescription($"{songs[0].Name} added.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                builder.WithDescription("Error when adding songs. Was that a youtube link?");
            }

            await FollowupAsync(embed: builder.Build());
        }

        [SlashCommand("skip", "Skip a song.", runMode: RunMode.Async)]
        public async Task Skip()
        {
            _musicModule.Skip(Context);
            await RespondAsync("Song Skipped");
        }

        [SlashCommand("skipmany", "Skip a number of songs.", runMode: RunMode.Async)]
        public async Task SkipManyAsync(int amount)
        {
            await DeferAsync();
            var builder = new EmbedBuilder();
            builder.WithColor(Color.Orange);

            if (amount <= 0)
            {
                builder.WithDescription($"Amount must be over 1");
                await FollowupAsync(embed: builder.Build(), ephemeral: true);
                return;
            }

            var songs = _musicModule.Queue(Context);
            _musicModule.Skip(Context, amount);

            builder.WithDescription($"**Skipped** {Math.Min(songs.Count, amount)}");

            await FollowupAsync(embed: builder.Build());
        }

        [SlashCommand("queue", "View the current queue.", runMode: RunMode.Async)]
        public async Task QueueAsync()
        {
            await DeferAsync();
            var songs = _musicModule.Queue(Context);
            var listAmount = 10;

            var builder = new EmbedBuilder();

            if (songs.Count > 0) 
            {
                builder.AddField("Now Playing", songs[0].Name);
            }

            string songString = $"**Queued Songs**{Environment.NewLine}";
            for (int i = 1; i < songs.Count - 1 && i <= listAmount; i++)
            {
                songString += $"**{i}.** {songs[i].Name}{Environment.NewLine}";
            }

            if (songs.Count > listAmount + 1)
            {
                songString += $"and {songs.Count - listAmount + 1} more";
            }

            builder.AddField(zeroWidthSpace, songString);
            builder.WithColor(Color.Orange);

            await FollowupAsync(embed: builder.Build());
        }

        [SlashCommand("clear", "Clears the queue of all songs.", runMode: RunMode.Async)]
        public async Task ClearAsync()
        {
            await DeferAsync();
            var songAmount = _musicModule.Queue(Context).Count;

            _musicModule.Clear(Context);

            var builder = new EmbedBuilder();
            builder.WithDescription($"Cleared {songAmount - 1} songs");
            builder.WithColor(Color.Orange);

            await FollowupAsync(embed: builder.Build());

        }
    }
}
