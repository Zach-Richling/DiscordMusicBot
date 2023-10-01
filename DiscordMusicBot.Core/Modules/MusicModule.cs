using CliWrap;
using Discord;
using Discord.Audio;
using Discord.Audio.Streams;
using Discord.Commands;
using DiscordMusicBot.Core.Data;
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
        private readonly MediaDownloader _youtubeDl;
        public MusicModule(MediaDownloader youtubeDl)
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
        public void Shuffle(IInteractionContext context) => GetOrAddGuild(context).Shuffle();

        private class GuildMusicHandler
        {
            private ulong _guildId;
            private List<Song> _queue;
            private Task? _queueTask;

            private CancellationTokenSource _tokenSource;
            private IAudioClient? _audioClient;


            private IInteractionContext _context;
            private readonly MediaDownloader _youtubeDl;

            private IUserMessage? _nowPlayingMessage;

            private object _lock = new();
            public GuildMusicHandler(IInteractionContext context, MediaDownloader youtubeDl, ulong guildId)
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

            public void Skip(int amount)
            {
                lock(_lock)
                {
                    if (_queue.Count < 1) 
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

            public void Shuffle()
            {
                var random = new Random();
                lock(_lock)
                {
                    int count = _queue.Count;
                    while (count > 2)
                    {
                        count--;
                        int index = random.Next(count + 1);
                        if (index == 0)
                        {
                            index++;
                        }

                        var value = _queue[index];
                        _queue[index] = _queue[count];
                        _queue[count] = value;
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
                    Log($"{_guildId}: Starting new queue task");
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
                            Log($"Removed {_queue[0].Name}");
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

                currentSong.AudioStream = await _youtubeDl.StreamAudio(currentSong, _tokenSource.Token);
                
                //If skip requested, download will cancel and return here.
                if (_tokenSource.Token.IsCancellationRequested)
                {
                    return;
                }

                await SendNowPlayingMessage(currentSong);

                if (currentSong.AudioStream == null)
                {
                    return;
                }

                try
                {
                    using (var ffmpegStream = await StartFFMPEG(currentSong.AudioStream))
                    using (var discordStream = _audioClient!.CreatePCMStream(AudioApplication.Mixed))
                    {
                        try
                        {
                            await discordStream.WriteAsync(ffmpegStream.ToArray(), 0, (int)ffmpegStream.Length, _tokenSource.Token);
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
                    try
                    {
                        await _nowPlayingMessage.DeleteAsync();
                    } catch { _nowPlayingMessage = null; }
                }

                var builder = new EmbedBuilder();

                builder.WithDescription($"**Now Playing:** {song.Name}");
                builder.WithColor(Color.Orange);

                _nowPlayingMessage = await _context.Channel.SendMessageAsync(embed: builder.Build());
            }

            private async Task<MemoryStream> StartFFMPEG(Stream audioStream)
            {
                MemoryStream ms = new MemoryStream();

                await Cli.Wrap("ffmpeg")
                    .WithArguments(" -hide_banner -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1")
                    .WithStandardInputPipe(PipeSource.FromStream(audioStream))
                    .WithStandardOutputPipe(PipeTarget.ToStream(ms))
                    .ExecuteAsync();

                return ms;
            }

            private void Log(string message)
            {
                Console.WriteLine($"{_guildId}: {message}");
            }
        }
        
    }
}
