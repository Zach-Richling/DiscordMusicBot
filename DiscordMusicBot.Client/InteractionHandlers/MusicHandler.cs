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
            var builder = InitializeEmbedBuilder();

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
            await DeferAsync();
            var builder = InitializeEmbedBuilder();
            builder.WithDescription($"Skipped Song");

            _musicModule.Skip(Context);

            await FollowupAsync(embed: builder.Build());
        }

        [SlashCommand("skipmany", "Skip a number of songs.", runMode: RunMode.Async)]
        public async Task SkipManyAsync(int amount)
        {
            await DeferAsync();
            var builder = InitializeEmbedBuilder();

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

            var builder = InitializeEmbedBuilder();

            if (songs.Count > 0) 
            {
                builder.AddField("Now Playing", songs[0].Name);
            }

            string songString = $"**Queued Songs**{Environment.NewLine}";
            for (int i = 1; i < songs.Count && i <= listAmount; i++)
            {
                songString += $"**{i}.** {songs[i].Name}{Environment.NewLine}";
            }

            if (songs.Count > listAmount + 1)
            {
                songString += $"and {songs.Count - listAmount + 1} more";
            }

            Console.WriteLine($"{Environment.NewLine}{string.Join(", ", songs.Select(x => x.Name))}");

            builder.AddField(zeroWidthSpace, songString);

            await FollowupAsync(embed: builder.Build());
        }

        [SlashCommand("clear", "Clears the queue of all songs.", runMode: RunMode.Async)]
        public async Task ClearAsync()
        {
            await DeferAsync();
            var songAmount = _musicModule.Queue(Context).Count;
            
            _musicModule.Clear(Context);

            var builder = InitializeEmbedBuilder();
            builder.WithDescription($"Cleared {songAmount - 1} songs");

            await FollowupAsync(embed: builder.Build());

        }

        [SlashCommand("shuffle", "Shuffle songs in queue", runMode: RunMode.Async)]
        public async Task ShuffleAsync()
        {
            await DeferAsync();

            _musicModule.Shuffle(Context);

            var builder = InitializeEmbedBuilder();
            builder.WithDescription($"Shuffled Queue");

            await FollowupAsync(embed: builder.Build());
        }

        private EmbedBuilder InitializeEmbedBuilder()
        {
            return new EmbedBuilder().WithColor(Color.Orange);
        }
    }
}
