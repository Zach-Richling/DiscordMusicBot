using Discord;
using Discord.Audio;
using Discord.Interactions;
using DiscordMusicBot.Core.Data;
using DiscordMusicBot.Core.Data.Youtube;
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

            if (url.StartsWith("https://www.youtube.com/playlist?"))
            {
                var videos = await _youtubeDl.ParsePlaylist(url).ToListAsync();
                foreach (var video in videos)
                {
                    _queue.Add(video);
                }
                await FollowupAsync(videos.Count() + " songs added to the queue.");
            }
            else
            {
                _queue.Add(url);
                await FollowupAsync(url + " added to the queue.");
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
            Console.WriteLine("In Queue");
            await RespondAsync("[" + String.Join(", ", _queue) + "]");
        }

        private async Task ProcessQueue(IAudioClient audioClient, IInteractionContext context, ulong guildId)
        {
            while(_queue.Any())
            {
                Console.WriteLine("Playing " + _queue.First());

                var cancellationTokenSource = new CancellationTokenSource();
                _cancellationTokens.TryAdd(guildId, cancellationTokenSource);

                var audioFile = await StartAudioAsync(audioClient, _queue.First(), cancellationTokenSource.Token);
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

        private async Task<string> StartAudioAsync(IAudioClient audioClient, string path, CancellationToken cancellationToken)
        {
            string filePath = "";
            try
            {
                filePath = await _youtubeDl.DownloadAudio(path);
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
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                }
            } 
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return filePath;
        }
    }
}
