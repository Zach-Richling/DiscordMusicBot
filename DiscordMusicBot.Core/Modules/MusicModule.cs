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
        public List<Song> Queue(IInteractionContext context) => GetOrAddGuild(context).GetQueue();

        private class GuildMusicHandler
        {
            private ulong _guildId;
            private List<Song> _queue;
            private Thread? _queueThread;
            private Task? _nextDownload;
            private CancellationTokenSource _tokenSource;
            private IAudioClient? _audioClient;


            private IInteractionContext _context;
            private readonly YoutubeDownloader _youtubeDl;

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
                    }
                    else
                    {
                        _queue.AddRange(songs);
                    }
                }

                StartQueueThread();
            }

            public void Skip() => _tokenSource.Cancel();

            public List<Song> GetQueue()
            {
                return _queue;
            }

            private void StartQueueThread()
            {
                if (_queueThread == null)
                {
                    Console.WriteLine($"{_guildId}: Starting new thread");
                    _queueThread = new Thread(async () => await ProcessQueue());
                    _queueThread.Start();
                }
            }

            private async Task ProcessQueue()
            {
                while (true) 
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
                            await StartAudioAsync(_queue.First(), null);
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
                            }
                        }
                    }
                    Thread.Sleep(1000);
                }
            }

            private async Task StartAudioAsync(Song song, int? bitRate)
            {
                var userChannel = (_context.User as IGuildUser)?.VoiceChannel;

                if (userChannel == null)
                {
                    return;
                }


                var progress = new Progress<double>(progress => Log(Math.Round(progress * 100, 2).ToString()));
                if (_audioClient == null || (_audioClient != null && (_audioClient.ConnectionState == ConnectionState.Disconnected || _audioClient.ConnectionState == ConnectionState.Disconnecting)))
                {
                    _audioClient = await userChannel.ConnectAsync();
                }

                Task? currentDownload = null;
                if (song.Downloading && _nextDownload != null && !_nextDownload.IsCompleted)
                {
                    Log("Waiting for next song");
                    currentDownload = _nextDownload;
                } 
                else if ((!song.Downloading && !song.Downloaded) || (song.Downloading && _nextDownload != null && _nextDownload.IsCanceled))
                {
                    Log("Downloading current song");
                    currentDownload = _youtubeDl.DownloadAudio(song, _tokenSource.Token, progress);
                }
                
                if (_queue.Count > 1 && !_queue[1].Downloaded && !_queue[1].Downloading)
                {
                    if (_nextDownload == null || (_nextDownload != null && _nextDownload.IsCompleted))
                    {
                        Log("Downloading next song");
                        _nextDownload = _youtubeDl.DownloadAudio(_queue[1], _tokenSource.Token, progress);
                    }
                }

                if (currentDownload != null) 
                {
                    await currentDownload;
                }

                if (_tokenSource.Token.IsCancellationRequested)
                {
                    return;
                }

                try
                {
                    using (Process ffmpeg = StartFFMPEG(song.FilePath, bitRate))
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
