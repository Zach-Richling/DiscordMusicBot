using CliWrap;
using Discord;
using Discord.Audio;
using Discord.Audio.Streams;
using Discord.Commands;
using Discord.WebSocket;
using DiscordMusicBot.Core.Data;
using DiscordMusicBot.Core.Enums;
using DiscordMusicBot.Core.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using YoutubeExplode.Videos.Streams;

namespace DiscordMusicBot.Core.Modules
{
    public class MusicModule
    {
        private ConcurrentDictionary<IGuild, GuildMusicModule> _guildHandlers = new();
        private readonly MediaDownloader _mediaDl;
        private readonly BaseFunctions _common;
        private readonly IConfiguration _config;

        public MusicModule(MediaDownloader mediaDl, BaseFunctions common, IConfiguration config)
        {
            _mediaDl = mediaDl;
            _common = common;
            _config = config;
        }
		
        private GuildMusicModule GetOrAddGuild(IInteractionContext context) 
        {
            return _guildHandlers.GetOrAdd(context.Guild, new GuildMusicModule(context, _mediaDl, _common, _config));
        }

        //Methods to route the received command to the correct guilds music handler.
        public async Task Play(IInteractionContext context, List<Song> songs, bool top, bool shuffle) => await GetOrAddGuild(context).Play(context, songs, top, shuffle);
        public async Task Skip(IInteractionContext context) => await GetOrAddGuild(context).Skip();
        public async Task Skip(IInteractionContext context, int amount) => await GetOrAddGuild(context).Skip(amount);
        public async Task Pause(IInteractionContext context) => await GetOrAddGuild(context).Pause();
        public async Task Resume(IInteractionContext context) => await GetOrAddGuild(context).Resume();
        public async Task<bool> Repeat(IInteractionContext context)
        {
            return await GetOrAddGuild(context).Repeat();
        }
        public async Task<bool> Join(IInteractionContext context)
        {
            return await GetOrAddGuild(context).Join(context);
        }
        public async Task<List<Song>> Queue(IInteractionContext context) => await GetOrAddGuild(context).GetQueue();
        public async Task<List<Song>> PreviousQueue(IInteractionContext context) => await GetOrAddGuild(context).GetPreviousQueue();
        public async Task Clear(IInteractionContext context) => await GetOrAddGuild(context).Clear();
        public async Task Shuffle(IInteractionContext context) => await GetOrAddGuild(context).Shuffle();

        public async Task Reset(IInteractionContext context)
        {
            _guildHandlers.Remove(context.Guild, out GuildMusicModule? handler);

            if (handler != null)
            {
                await handler.Clear();
                await handler.Skip();
            }

            await Task.CompletedTask;
        }

        public async Task RemoveAllHandlers()
        {
            foreach (var guildHandler in _guildHandlers.Select(x => x.Value))
            {
                await guildHandler.Clear();
                await guildHandler.Skip();
            }
            _guildHandlers.Clear();
        }
        
        public async Task UserVoiceEvent(SocketUser socketUser, SocketVoiceState stateBefore, SocketVoiceState stateAfter)
        {
            await Task.Run(async () => {
                var voiceChannel = stateBefore.VoiceChannel;
                var guild = stateBefore.VoiceChannel?.Guild;

                if (guild != null && _guildHandlers.TryGetValue(guild, out var guildMusicModule))
                {
                    if (voiceChannel != null)
                    {
                        var currentVC = guildMusicModule.GetCurrentVoiceChannel();
                        if (currentVC != stateAfter.VoiceChannel && currentVC != null)
                        {
                            var users = voiceChannel.ConnectedUsers;

                            if (!users.Where(x => !x.IsBot).Any())
                            {
                                await guildMusicModule.Clear();
                                await guildMusicModule.Skip();
                                _guildHandlers.Remove(guild, out var _);
                            }
                        }
                    }
                }
            });
        }

        private class GuildMusicModule
        {
            private IGuild _guild;
            private List<Song> _queue;
            private List<Song> _previous;
            private Task? _queueTask;

            private CancellationTokenSource _tokenSource;
            private IAudioClient? _audioClient;


            private IVoiceChannel? _requestedVC;
            private IMessageChannel _messageChannel;
            private readonly MediaDownloader _mediaDl;
            private readonly BaseFunctions _common;
            private readonly IConfiguration _config;

            private object _lock = new();
            private SongAction _songAction = SongAction.None;

            public GuildMusicModule(IInteractionContext context, MediaDownloader mediaDl, BaseFunctions common, IConfiguration config)
            {
                _requestedVC = ((IGuildUser)context.User).VoiceChannel;
                _messageChannel = context.Channel;
                _mediaDl = mediaDl;
                _common = common;
                _guild = context.Guild;
                _config = config;
                _queue = new();
                _previous = new();
                _tokenSource = new();
            }

            public async Task Play(IInteractionContext context, List<Song> songs, bool top, bool shuffle)
            {
                if (songs.Count > 1 && shuffle)
                {
                    var random = new Random();
                    int count = songs.Count;
                    while (count > 2)
                    {
                        count--;
                        int index = random.Next(count + 1);

                        var value = songs[index];
                        songs[index] = songs[count];
                        songs[count] = value;
                    }
                }

                lock (_lock) 
                {
                    if (top && _queue.Count != 0)
                    {
                        _queue.InsertRange(1, songs);
                    }
                    else
                    {
                        _queue.AddRange(songs);
                    }
                }

                if (_audioClient == null)
                {
                    _requestedVC = ((IGuildUser)context.User).VoiceChannel;
                }

                await StartQueueThread();
            }

            public async Task<bool> Join(IInteractionContext context)
            {
                if (_queue.Count != 0)
                {
                    _requestedVC = ((IGuildUser)context.User).VoiceChannel;
                    _queue.Insert(1, _queue[0]);

                    if (_audioClient != null)
                    {
                        await _audioClient.StopAsync();
                        _audioClient.Dispose();
                        _audioClient = null;
                    }

                    await Skip();
                    return true;
                }

                return false;
            }

            public async Task Skip()
            {
                _songAction = SongAction.Skip;
                _tokenSource.Cancel();
                await Task.CompletedTask;
            }

            public async Task Skip(int amount)
            {
                lock(_lock)
                {
                    if (_queue.Count > 1) 
                    {
                        _queue.RemoveRange(1, Math.Min(amount - 1, _queue.Count - 1));
                        _songAction = SongAction.Skip;
                        _tokenSource.Cancel();
                    }
                }

                await Task.CompletedTask;
            }

            public async Task Pause()
            {
                _songAction = SongAction.Pause;
                _tokenSource.Cancel();

                await Task.CompletedTask;
            }

            public async Task<bool> Repeat()
            {
                if (_songAction != SongAction.Repeat)
                {
                    _songAction = SongAction.Repeat;
                    return await Task.FromResult(true);
                } 
                else
                {
                    _songAction = SongAction.None;
                    return await Task.FromResult(false);
                }
            }

            public async Task Resume()
            {
                _songAction = SongAction.Resume;
                await Task.CompletedTask;
            }

            public async Task Clear()
            {
                lock (_lock)
                {
                    if (_queue.Count > 1) 
                    {
                        _queue.RemoveRange(1, _queue.Count - 1);
                    }
                }

                await Task.CompletedTask;
            }

            public async Task Shuffle()
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

                await Task.CompletedTask;
            }

            public async Task<List<Song>> GetQueue()
            {
                return await Task.FromResult(_queue);
            }

            public IVoiceChannel? GetCurrentVoiceChannel()
            {
                if (_audioClient == null)
                {
                    return null;
                }

                return _requestedVC;
            }

            public async Task<List<Song>> GetPreviousQueue()
            {
                return await Task.FromResult(_previous);
            }

            private async Task StartQueueThread()
            {
                if (_queueTask == null || _queueTask.IsCompleted)
                {
                    Log("Starting new queue task");
                    _queueTask = ProcessQueue();
                }

                await Task.CompletedTask;
            }
            
            private async Task ProcessQueue()
            {
                while (_queue.Count != 0)
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

                            if (_previous.Count != 10)
                            {
                                _previous.Add(_queue[0]);
                            }
                            else
                            {
                                _previous.RemoveAt(0);
                                _previous.Add(_queue[0]);
                            }

                            _queue.RemoveAt(0);
                            _queue.TrimExcess();
                            GC.Collect();
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
                _songAction = SongAction.None;

                Song? currentSong;

                lock(_lock)
                {
                    currentSong = _queue.FirstOrDefault();
                }

                if (_requestedVC == null || currentSong == null)
                {
                    return;
                }

                if (_audioClient == null || (_audioClient != null && (_audioClient.ConnectionState == ConnectionState.Disconnected || _audioClient.ConnectionState == ConnectionState.Disconnecting)))
                {
                    _audioClient = await _requestedVC.ConnectAsync();
                }

                var timer = Stopwatch.StartNew();
                Log("Getting audio stream");
                using Stream audioStream = await _mediaDl.StreamAudio(currentSong, _tokenSource.Token);
                Log($"Finished getting audio stream: {timer.Elapsed.TotalSeconds}");
                timer.Stop();

                IUserMessage nowPlayingMessage = await SendNowPlayingMessage(currentSong);

                int playCount = 0;
                using (var ffmpegStream = await StartFFMPEG(audioStream, _tokenSource.Token))
                using (var discordStream = _audioClient!.CreatePCMStream(AudioApplication.Mixed))
                {
                    while ((!_tokenSource.IsCancellationRequested && _songAction == SongAction.Repeat) || (_songAction != SongAction.Repeat && playCount == 0))
                    {
                        playCount++;

                        if (_songAction == SongAction.Repeat)
                        {
                            ffmpegStream.Seek(0, SeekOrigin.Begin);
                        }

                        try
                        {
                            try
                            {
                                timer.Restart();
                                Log("Writing audio to discord");
                                int currentPosition;
                                byte[] buffer = new byte[4096];
                                while ((currentPosition = await ffmpegStream.ReadAsync(buffer)) > 0)
                                {
                                    await discordStream.WriteAsync(buffer.AsMemory(0, currentPosition), _tokenSource.Token);
                                }
                                Log($"Finished writing audio to discord: {timer.Elapsed.TotalSeconds}");
                            }
                            finally
                            {
                                await discordStream.FlushAsync(_tokenSource.Token);
                            }
                        } 
                        catch (OperationCanceledException) { }

                        if (_songAction == SongAction.Pause)
                        {
                            while (_songAction != SongAction.Resume)
                            {
                                Thread.Sleep(100);
                            }

                            _tokenSource = new();
                            playCount = 0;
                        }
                    }
                }

                try
                {
                    await nowPlayingMessage.DeleteAsync();
                }
                catch { }
            }

            private async Task<IUserMessage> SendNowPlayingMessage(Song song)
            {
                var builder = _common.InitializeEmbedBuilder();
                builder.WithDescription($"**Now Playing:** {_common.NameWithEmoji(song)}");
                return await _messageChannel.SendMessageAsync(embed: builder.Build());
            }

            private async Task<Stream> StartFFMPEG(Stream audioStream, CancellationToken cancellationToken)
            {
                var timer = Stopwatch.StartNew();
                Log("Transcoding with FFMPEG");
                MemoryStream ms = new MemoryStream();

                await Cli.Wrap("ffmpeg")
                    .WithArguments(" -hide_banner -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1")
                    .WithStandardInputPipe(PipeSource.FromStream(audioStream))
                    .WithStandardOutputPipe(PipeTarget.ToStream(ms))
                    .ExecuteAsync(cancellationToken);

                ms.Seek(0, SeekOrigin.Begin);

                Log($"Finished transcoding with FFMPEG: {timer.Elapsed.TotalSeconds}");
                timer.Stop();

                return ms;
            }

            private void Log(string message, string loglevel = "Info")
            {
                Console.WriteLine($"[MusicModule/{loglevel}] {DateTime.Now.ToString("HH:mm:ss")} {_guild.Id}: {message}");
            }
        }
        
    }
}
