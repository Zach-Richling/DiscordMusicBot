using Discord;
using Discord.Audio;
using DiscordMusicBot.Core.Data.Youtube;
using DiscordMusicBot.Core.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using YoutubeExplode.Videos.Streams;

namespace DiscordMusicBot.Core.Modules
{
    public class MusicModule
    {
        private ConcurrentDictionary<ulong, GuildMusicHandler> _guildHandlers = new();
        private readonly YoutubeDownloader _youtubeDl;
        public MusicModule(YoutubeDownloader youtubeDl)
        {
            _youtubeDl = youtubeDl;
        }

        private GuildMusicHandler GetOrAddGuild(IInteractionContext context) 
        {
            return _guildHandlers.GetOrAdd(context.Guild.Id, new GuildMusicHandler(context, _youtubeDl, context.Guild.Id));
        }

        public void Play(IInteractionContext context, List<Song> songs, bool top) => GetOrAddGuild(context).Play(songs, top);
        public void Skip(IInteractionContext context) => GetOrAddGuild(context).Skip();
        public void Skip(IInteractionContext context, int amount) => GetOrAddGuild(context).Skip(amount);
        public List<Song> Queue(IInteractionContext context) => GetOrAddGuild(context).GetQueue();
        public void Clear(IInteractionContext context) => GetOrAddGuild(context).Clear();

        private class GuildMusicHandler
        {
            private ulong _guildId;
            private List<Song> _queue;
            private Task? _queueTask;

            private CancellationTokenSource _tokenSource;
            private IAudioClient? _audioClient;


            private IInteractionContext _context;
            private readonly YoutubeDownloader _youtubeDl;

            private IUserMessage _nowPlayingMessage;

            private object _lock = new();
            public GuildMusicHandler(IInteractionContext context, YoutubeDownloader youtubeDl, ulong guildId)
            {
                _context = context;
                _youtubeDl = youtubeDl;
                _guildId = guildId;
                _queue = new();
                _tokenSource = new();
            }

            public void Play(List<Song> songs, bool top)
            {
                lock (_lock) 
                {
                    if (top && _queue.Any())
                    {
                        _queue.InsertRange(1, songs);
                        _queue[1].Download = _youtubeDl.DownloadAudio(_queue[1], _queue[1].CancellationTokenSource.Token);
                    }
                    else
                    {
                        _queue.AddRange(songs);
                    }
                }

                StartQueueThread();
            }

            public void Skip() => _tokenSource.Cancel();

            public void Skip(int amount)
            {
                lock(_lock)
                {
                    if (_queue.Count < 0) 
                    {
                        _queue.RemoveRange(1, Math.Min(amount - 1, _queue.Count));
                        _tokenSource.Cancel();
                    }
                }
            }

            public void Clear()
            {
                lock (_lock)
                {
                    if (_queue.Count > 1) 
                    {
                        _queue.RemoveRange(1, _queue.Count - 1);
                    }
                }
            }

            public List<Song> GetQueue()
            {
                return _queue;
            }

            private void StartQueueThread()
            {
                if (_queueTask == null || _queueTask.IsCompleted)
                {
                    Console.WriteLine($"{_guildId}: Starting new thread");
                    _queueTask = Task.Run(() => ProcessQueue());
                }
            }
            
            private async Task ProcessQueue()
            {
                while (_queue.Any())
                {
                    try
                    {
                        if (_tokenSource.IsCancellationRequested)
                        {
                            _tokenSource = new();
                        }

                        Log($"Processing: {_queue.First().Name}");
                        await StartAudioAsync();
                    }
                    catch (Exception e)
                    {
                        Log(e.ToString());
                    }
                    finally
                    {
                        lock (_lock)
                        {
                            _queue.RemoveAt(0);
                        }

                        if (!_queue.Any())
                        {
                            Log("Finished Queue");
                            if (_audioClient != null)
                            {
                                await _audioClient.StopAsync();
                                _audioClient.Dispose();
                                _audioClient = null;
                            }
                        }
                    }
                }
            }

            private async Task StartAudioAsync()
            {
                var userChannel = (_context.User as IGuildUser)?.VoiceChannel;

                Song currentSong = _queue[0];
                var nextSong = _queue.Count > 1 ? _queue[1] : null;

                if (userChannel == null)
                {
                    return;
                }

                if (_audioClient == null || (_audioClient != null && (_audioClient.ConnectionState == ConnectionState.Disconnected || _audioClient.ConnectionState == ConnectionState.Disconnecting)))
                {
                    _audioClient = await userChannel.ConnectAsync();
                }

                if (currentSong.Download != null)
                {
                    currentSong.CancellationTokenSource = _tokenSource;
                }

                //Start current song download
                if (currentSong.Download == null)
                {
                    currentSong.CancellationTokenSource = _tokenSource;
                    currentSong.Download = _youtubeDl.DownloadAudio(currentSong, currentSong.CancellationTokenSource.Token);
                }

                //Start next song download
                if (nextSong != null && nextSong.Download == null)
                {
                    nextSong.Download = _youtubeDl.DownloadAudio(nextSong, nextSong.CancellationTokenSource.Token);
                }

                //Wait for current song download to finish
                if (currentSong.Download != null)
                {
                    if (_tokenSource.IsCancellationRequested)
                    {
                        currentSong.CancellationTokenSource.Cancel();
                    }

                    await currentSong.Download;
                }

                //If skip requested, download will cancel and return here.
                if (_tokenSource.Token.IsCancellationRequested)
                {
                    return;
                }

                await SendNowPlayingMessage(currentSong);

                try
                {
                    using (Process ffmpeg = StartFFMPEG(currentSong.FilePath))
                    using (var outputStream = ffmpeg.StandardOutput.BaseStream)
                    using (var discordStream = _audioClient!.CreatePCMStream(AudioApplication.Mixed))
                    {
                        try
                        {
                            if (outputStream != null)
                            {
                                await outputStream.CopyToAsync(discordStream, _tokenSource.Token);
                            }
                        }
                        finally
                        {
                            await discordStream.FlushAsync(_tokenSource.Token);
                        }
                    }
                } 
                catch (OperationCanceledException)
                {
                    //Do nothing if operation is cancelled
                }
            }

            private async Task SendNowPlayingMessage(Song song)
            {
                if (_nowPlayingMessage != null)
                {
                    await _nowPlayingMessage.DeleteAsync();
                }

                var builder = new EmbedBuilder();

                builder.WithDescription($"**Now Playing:** {song.Name}");
                builder.WithColor(Color.Orange);

                _nowPlayingMessage = await _context.Channel.SendMessageAsync(embed: builder.Build());
            }

            private Process StartFFMPEG(string path, int? bitRate = null)
            {
                var ffmpeg = Process.Start(new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar {bitRate ?? 48000} pipe:1",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                });

                if (ffmpeg == null)
                {
                    throw new NullReferenceException("FFMPEG could not start");
                }

                return ffmpeg;
            }

            private void Log(string message)
            {
                Console.WriteLine($"{_guildId}: {message}");
            }
        }
        
    }
}
