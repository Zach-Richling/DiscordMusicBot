﻿using Discord;
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
        private readonly MediaDownloader _mediaDl;
        private readonly BaseFunctions _common;
        private readonly IConfiguration _config;
        private readonly string zeroWidthSpace = '\u200b'.ToString();

        public MusicHandler(MusicModule musicModule, MediaDownloader mediaDl, BaseFunctions common, IConfiguration appConfig)
        {
            _musicModule = musicModule;
            _mediaDl = mediaDl;
            _common = common;
            _config = appConfig;
        }

        [SlashCommand("play", "Play a song.", runMode: RunMode.Async)]
        public async Task PlayAsync(string url)
        {
            await PlayInnerAsync(url, false, false);
        }

        [SlashCommand("play-top", "Play a song at the top of the queue.", runMode: RunMode.Async)]
        public async Task PlayTopAsync(string url)
        {
            await PlayInnerAsync(url, true, false);
        }

        [SlashCommand("play-shuffle", "Shuffle a playlist before adding to the queue.", runMode: RunMode.Async)]
        public async Task PlayShuffleAsync(string url)
        {
            await PlayInnerAsync(url, false, true);
        }

        [SlashCommand("play-top-shuffle", "Shuffle a playlist before adding to the top of the queue.", runMode: RunMode.Async)]
        public async Task PlayTopShuffleAsync(string url)
        {
            await PlayInnerAsync(url, true, true);
        }

        [SlashCommand("play-previous", "Add a previously played song to the queue.", runMode: RunMode.Async)]
        public async Task PlayPreviousAsync(int index)
        {
            await PlayPreviousInnerAsync(index, false);
        }

        [SlashCommand("play-top-previous", "Add a previously played song to the top of the queue.", runMode: RunMode.Async)]
        public async Task PlayTopPreviousAsync(int index)
        {
            await PlayPreviousInnerAsync(index, true);
        }

        private async Task PlayPreviousInnerAsync(int index, bool top)
        {
            await DeferAsync();
            var previousQueue = await _musicModule.PreviousQueue(Context);
            var builder = _common.InitializeEmbedBuilder();
            if (previousQueue.Count == 0)
            {
                builder.WithDescription($"No songs in previous queue");
            }
            else if (previousQueue.Count == 1 && index != 1)
            {
                builder.WithDescription($"Index must be 1");
            }
            else if (index < 1 || index > previousQueue.Count)
            {
                builder.WithDescription($"Index must be between 1 and {previousQueue.Count}");
            }

            if (!string.IsNullOrEmpty(builder.Description))
            {
                await FollowupAsync(embed: builder.Build());
                return;
            }

            var song = previousQueue[index - 1];
            await _musicModule.Play(Context, new List<Song> { song }, top, false);
            builder.WithDescription($"{_common.NameWithEmoji(song)} added.");
            await FollowupAsync(embed: builder.Build());
        }

        private async Task PlayInnerAsync(string url, bool top, bool shuffle)
        {
            await DeferAsync();
            var builder = _common.InitializeEmbedBuilder();

            try
            {
                var songs = await _mediaDl.ProcessURL(url);
                await _musicModule.Play(Context, songs, top, shuffle);

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
                builder.WithDescription(e.Message);
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

        [SlashCommand("skip-many", "Skip a number of songs.", runMode: RunMode.Async)]
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

            var songCount = (await _musicModule.Queue(Context)).Count;
            await _musicModule.Skip(Context, amount);

            builder.WithDescription($"Skipped {Math.Min(songCount, amount)} songs");

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

            var songString = "";

            if (songs.Count > 0)
            {
                songString += $"**Now Playing**{Environment.NewLine}";
                songString += $"{_common.NameWithEmoji(songs[0])} ({songs[0].Length.ToString("hh':'mm':'ss")})";
            }

            if (songs.Count > 1)
            {
                songString += $"{Environment.NewLine}{Environment.NewLine}**Queued Songs**{Environment.NewLine}";
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

            builder.WithDescription(songString);
            await FollowupAsync(embed: builder.Build());
        }

        [SlashCommand("previous-queue", "Display previous songs in the queue.", runMode: RunMode.Async)]
        public async Task PreviousQueueAsync()
        {
            await DeferAsync();
            var previousSongs = await _musicModule.PreviousQueue(Context);
            var builder = _common.InitializeEmbedBuilder();
            string songString = "";

            if (previousSongs.Count == 0)
            {
                builder.WithDescription("No songs in previous queue.");
                await FollowupAsync(embed: builder.Build());
                return;
            }

            songString = $"**Previous Songs**{Environment.NewLine}";
            for (int i = 0; i < previousSongs.Count; i++)
            {
                songString += $"**{i + 1}.** {_common.NameWithEmoji(previousSongs[i])}{Environment.NewLine}";
            }

            builder.WithDescription(songString);
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

        [SlashCommand("join", "Makes the bot join the users channel", runMode: RunMode.Async)]
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
