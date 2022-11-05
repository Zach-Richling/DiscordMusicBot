using AngleSharp.Dom;
using Discord;
using Discord.Audio;
using Discord.Interactions;
using DiscordMusicBot.Core.Data;
using DiscordMusicBot.Core.Data.Youtube;
using DiscordMusicBot.Core.Models;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordMusicBot.Client.InteractionHandlers
{
    public class QueueHandler : InteractionModuleBase
    {
        private QueueList _queue;
        private readonly YoutubeDownloader _youtubeDl;
        private static ConcurrentDictionary<ulong, CancellationTokenSource> _cancellationTokens = new ConcurrentDictionary<ulong, CancellationTokenSource>();
        public QueueHandler(QueueList queue, YoutubeDownloader youtubeDl)
        {
            _queue = queue;
            _youtubeDl = youtubeDl;
        }

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
            if (_queue.Count() == 0)
            {
                await PlayInnerAsync(url, false);
            } else 
            {
                await PlayInnerAsync(url, true);
            }
        }

        private async Task PlayInnerAsync(string url, bool playTop)
        {
            await DeferAsync();

            var userChannel = (Context.User as IGuildUser)?.VoiceChannel;
            var guildId = Context.Interaction.GuildId;

            if (userChannel == null)
            {
                await Context.Channel.SendMessageAsync("You must be in a voice channel to play a song.");
                return;
            }

            bool joinChannel = false;

            if (_queue.Count() == 0)
            {
                joinChannel = true;
            }

            if (url.StartsWith("https://www.youtube.com/playlist?") || url.Contains("&list"))
            {
                var songs = await _youtubeDl.ParsePlaylist(url).ToListAsync();
                
                if (playTop)
                {
                    _queue.InsertRange(1, songs);
                }
                else
                {
                    _queue.AddRange(songs);
                }
                
                await FollowupAsync(songs.Count() + " songs added to the queue.");
            }
            else
            {
                var song = await _youtubeDl.CreateSong(url);
                if (playTop)
                {
                    _queue.Insert(1, song);
                } 
                else 
                {
                    _queue.Add(song);
                }
                await FollowupAsync(song.Name + " added to the queue.");
            }

            //If this is the first song to play, connect to the users voice chat and start processing the queue.
            if (joinChannel)
            {
                if (guildId.HasValue)
                {
                    var audioClient = await userChannel.ConnectAsync();
                    await ProcessQueue(audioClient, Context, guildId.Value);
                    await audioClient.StopAsync();
                }
            }
        }

        [SlashCommand("skip", "Skip a song.", runMode: RunMode.Async)]
        public async Task SkipAsync()
        {
            await RespondAsync("Skipped");
            var guildId = Context.Interaction.GuildId;
            if (guildId.HasValue)
            {
                _cancellationTokens.TryGetValue(guildId.Value, out var cancellationTokenSource);
                if (cancellationTokenSource != null)
                {
                    cancellationTokenSource.Cancel();
                }
            }
        }

        [SlashCommand("queue", "View the current queue.")]
        public async Task QueueAsync()
        {
            if (!_queue.Any())
            {
                await RespondAsync("No Songs in Queue.");
                return;
            }

            var response = "Now Playing: " + _queue.First().Name;
            response += Environment.NewLine + "Upcoming: " + Environment.NewLine;
            response += string.Join(Environment.NewLine, _queue.Skip(1).Take(5).Select(x => x.Name));

            TimeSpan totalTime = new TimeSpan(_queue.Sum(x => x.Length.Ticks));
            response += Environment.NewLine + "Total Length: " + totalTime.ToString();
            await RespondAsync(response);
        }

        private async Task ProcessQueue(IAudioClient audioClient, IInteractionContext context, ulong guildId)
        {
            while(_queue.Any())
            {
                Console.WriteLine("Playing " + _queue.First().Name);

                var cancellationTokenSource = new CancellationTokenSource();
                _cancellationTokens.TryAdd(guildId, cancellationTokenSource);

                await StartAudioAsync(audioClient, _queue.First(), cancellationTokenSource.Token);
                _queue.RemoveAt(0);

                _cancellationTokens.Remove(guildId, out var oldCancellationSource);
            }
        }

        private Process? StartFFMPEG(string path)
        {
            try
            {
                return Process.Start(new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                });
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private async Task StartAudioAsync(IAudioClient audioClient, Song song, CancellationToken cancellationToken)
        {
            string filePath = "";
            try
            {
                filePath = await _youtubeDl.DownloadAudio(song.Url);
                using (var ffmpeg = StartFFMPEG(filePath))
                using (var outputStream = ffmpeg?.StandardOutput.BaseStream)
                using (var discordStream = audioClient.CreatePCMStream(AudioApplication.Mixed))
                {
                    try
                    {
                        if (outputStream != null)
                        {
                            await outputStream.CopyToAsync(discordStream, cancellationToken);
                        }
                    }
                    finally
                    {
                        try
                        {
                            await discordStream.FlushAsync(cancellationToken);
                        }
                        catch (OperationCanceledException e)
                        {
                            //When FlushAsync is cancelled it throws an OperationCancelledException
                        }
                    }
                }
            } 
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
