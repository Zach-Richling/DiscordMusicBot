using Discord;
using Discord.Interactions;
using DiscordMusicBot.Core.Data;
using DiscordMusicBot.Core.Enums;
using DiscordMusicBot.Core.Models;
using DiscordMusicBot.Core.Modules;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography.X509Certificates;

namespace DiscordMusicBot.Client.InteractionHandlers
{
    public class MusicHandler : InteractionModuleBase
    {
        private readonly MusicModule _musicModule;
        private readonly MediaDownloader _youtubeDl;
        private readonly BaseFunctions _common;
        private readonly IConfiguration _config;
        private readonly string zeroWidthSpace = '\u200b'.ToString();

        public MusicHandler(MusicModule musicModule, MediaDownloader youtubeDl, BaseFunctions common, IConfiguration appConfig)
        {
            _musicModule = musicModule;
            _youtubeDl = youtubeDl;
            _common = common;
            _config = appConfig;
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
            var builder = _common.InitializeEmbedBuilder();

            try
            {
                var songs = await _youtubeDl.ProcessURL(url);
                await _musicModule.Play(Context, songs, top);

                if (songs.Count == 1)
                {
                    builder.WithDescription($"{_common.NameWithEmoji(songs[0])} added.");
                }
                else
                {
                    builder.WithDescription($"{songs.Count} songs added.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                builder.WithDescription("Error when adding songs. Was that a youtube/soundcloud link?");
            }

            await FollowupAsync(embed: builder.Build());
        }

        [SlashCommand("skip", "Skip a song.", runMode: RunMode.Async)]
        public async Task Skip()
        {
            await DeferAsync();
            var builder = _common.InitializeEmbedBuilder();
            builder.WithDescription("Song Skipped");
            await _musicModule.Skip(Context);

            await FollowupAsync(embed: builder.Build());
        }

        [SlashCommand("skipmany", "Skip a number of songs.", runMode: RunMode.Async)]
        public async Task SkipManyAsync(int amount)
        {
            await DeferAsync();
            var builder = _common.InitializeEmbedBuilder();

            if (amount <= 0)
            {
                builder.WithDescription($"Amount must be over 1");
                await FollowupAsync(embed: builder.Build(), ephemeral: true);
                return;
            }

            var songs = await _musicModule.Queue(Context);
            await _musicModule.Skip(Context, amount);

            builder.WithDescription($"**Skipped** {Math.Min(songs.Count, amount)}");

            await FollowupAsync(embed: builder.Build());
        }

        [SlashCommand("queue", "View the current queue.", runMode: RunMode.Async)]
        public async Task QueueAsync()
        {
            await DeferAsync();
            var songs = await _musicModule.Queue(Context);
            var listAmount = 10;

            var builder = _common.InitializeEmbedBuilder();

            if (songs.Count == 0)
            {
                builder.WithDescription("No songs queued.");
                await FollowupAsync(embed: builder.Build());
                return;
            }

            if (songs.Count > 0) 
            {
                builder.AddField("Now Playing", $"{_common.NameWithEmoji(songs[0])} ({songs[0].Length.ToString("hh':'mm':'ss")})");
            }

            var songString = "";

            if (songs.Count > 1) 
            {
                songString = $"**Queued Songs**{Environment.NewLine}";
                for (int i = 1; i < songs.Count && i <= listAmount; i++)
                {
                    songString += $"**{i}.** {_common.NameWithEmoji(songs[i])} ({songs[i].Length.ToString("hh':'mm':'ss")}){Environment.NewLine}";
                }
            }

            var totalTime = new TimeSpan();
            foreach (var time in songs.Select(x => x.Length))
            {
                totalTime = totalTime.Add(time);
            }

            if (songs.Count > listAmount + 1)
            {
                songString += $"and {songs.Count - listAmount - 1} more{Environment.NewLine}";
            }

            songString += $"{Environment.NewLine}Total Time: {totalTime.ToString("hh':'mm':'ss")}";

            builder.AddField(zeroWidthSpace, songString);

            await FollowupAsync(embed: builder.Build());
        }

        [SlashCommand("clear", "Clears the queue of all songs.", runMode: RunMode.Async)]
        public async Task ClearAsync()
        {
            await DeferAsync();
            var songAmount = (await _musicModule.Queue(Context)).Count;
            
            await _musicModule.Clear(Context);

            var builder = _common.InitializeEmbedBuilder();

            if (songAmount != 0) 
            {
                builder.WithDescription($"Cleared {songAmount - 1} songs");
            } 
            else
            {
                builder.WithDescription("No songs to clear");
            }

            await FollowupAsync(embed: builder.Build());

        }

        [SlashCommand("shuffle", "Shuffle songs in queue", runMode: RunMode.Async)]
        public async Task ShuffleAsync()
        {
            await DeferAsync();

            await _musicModule.Shuffle(Context);

            var builder = _common.InitializeEmbedBuilder();
            builder.WithDescription($"Shuffled Queue");

            await FollowupAsync(embed: builder.Build());
        }

        [SlashCommand("pause", "Pause the current song.", runMode: RunMode.Async)]
        public async Task PauseAsync()
        {
            await DeferAsync();

            await _musicModule.Pause(Context);

            var builder = _common.InitializeEmbedBuilder();
            builder.WithDescription($"Song Paused");

            await FollowupAsync(embed: builder.Build());
        }

        [SlashCommand("resume", "Resumes the current song.", runMode: RunMode.Async)]
        public async Task ResumeAsync()
        {
            await DeferAsync();

            await _musicModule.Resume(Context);

            var builder = _common.InitializeEmbedBuilder();
            builder.WithDescription($"Song Resumed");

            await FollowupAsync(embed: builder.Build());
        }

        [SlashCommand("repeat", "Toggles repeating the current song.", runMode: RunMode.Async)]
        public async Task RepeatAsync()
        {
            await DeferAsync();

            var repeated = await _musicModule.Repeat(Context);

            var builder = _common.InitializeEmbedBuilder();
            builder.WithDescription($"Repeat toggled " + (repeated ? "on" : "off"));

            await FollowupAsync(embed: builder.Build());
        }

        [SlashCommand("join", "Makes the bot join to the users channel", runMode: RunMode.Async)]
        public async Task JoinAsync()
        {
            await DeferAsync();

            var joined = await _musicModule.Join(Context);

            var builder = _common.InitializeEmbedBuilder();
            
            if (joined)
            {
                builder.WithDescription($"Changed channels");
            }
            else
            {
                builder.WithDescription($"Could not change channels");
            }

            await FollowupAsync(embed: builder.Build());
        }

        [SlashCommand("reset", "Destroys all resources used by the bot on the server.", runMode: RunMode.Async)]
        public async Task ResetAsync()
        {
            await DeferAsync();

            await _musicModule.Reset(Context);

            var builder = _common.InitializeEmbedBuilder();
            builder.WithDescription($"Destroyed all resources used by bot.");

            await FollowupAsync(embed: builder.Build());
        }
    }
}
