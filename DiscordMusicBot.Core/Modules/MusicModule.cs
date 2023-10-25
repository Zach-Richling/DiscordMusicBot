using CliWrap;
using Discord;
using Discord.Audio;
using Discord.Audio.Streams;
using Discord.Commands;
using DiscordMusicBot.Core.Data;
using DiscordMusicBot.Core.Enums;
using DiscordMusicBot.Core.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using YoutubeExplode.Videos.Streams;

namespace DiscordMusicBot.Core.Modules
{
    public class MusicModule
    {
        private ConcurrentDictionary<ulong, GuildMusicModule> _guildHandlers = new();
        private readonly MediaDownloader _mediaDl;
        private readonly BaseFunctions _common;
        public MusicModule(MediaDownloader mediaDl, BaseFunctions common)
        {
            _mediaDl = mediaDl;
            _common = common;
        }

        private GuildMusicModule GetOrAddGuild(IInteractionContext context) 
        {
            return _guildHandlers.GetOrAdd(context.Guild.Id, new GuildMusicModule(context, _mediaDl, _common, context.Guild.Id));
        }

        public async Task Play(IInteractionContext context, List<Song> songs, bool top) => await GetOrAddGuild(context).Play(songs, top);
        public async Task Skip(IInteractionContext context) => await GetOrAddGuild(context).Skip();
        public async Task Skip(IInteractionContext context, int amount) => await GetOrAddGuild(context).Skip(amount);
        public async Task Pause(IInteractionContext context) => await GetOrAddGuild(context).Pause();
        public async Task Resume(IInteractionContext context) => await GetOrAddGuild(context).Resume();
        public async Task<bool> Repeat(IInteractionContext context)
        {
            return await GetOrAddGuild(context).Repeat();
        }
        public async Task<List<Song>> Queue(IInteractionContext context) => await GetOrAddGuild(context).GetQueue();
        public async Task Clear(IInteractionContext context) => await GetOrAddGuild(context).Clear();
        public async Task Shuffle(IInteractionContext context) => await GetOrAddGuild(context).Shuffle();

        public async Task Reset(IInteractionContext context)
        {
            _guildHandlers.Remove(context.Guild.Id, out GuildMusicModule? handler);

            if (handler != null)
            {
                await handler.Clear();
                await handler.Skip();
            }

            await Task.CompletedTask;
        }

        private class GuildMusicModule
        {
            private ulong _guildId;
            private List<Song> _queue;
            private Task? _queueTask;

            private CancellationTokenSource _tokenSource;
            private IAudioClient? _audioClient;


            private IInteractionContext _context;
            private readonly MediaDownloader _mediaDl;
            private readonly BaseFunctions _common;

            private object _lock = new();
            private bool _repeat = false;
            private SongAction _songAction = SongAction.None;

            public GuildMusicModule(IInteractionContext context, MediaDownloader mediaDl, BaseFunctions common, ulong guildId)
            {
                _context = context;
                _mediaDl = mediaDl;
                _common = common;
                _guildId = guildId;
                _queue = new();
                _tokenSource = new();
            }

            public async Task Play(List<Song> songs, bool top)
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

                await StartQueueThread();
            }

            public async Task Skip()
            {
                _tokenSource.Cancel();
                _songAction = SongAction.Skip;
                await Task.CompletedTask;
            }

            public async Task Skip(int amount)
            {
                lock(_lock)
                {
                    if (_queue.Count < 1) 
                    {
                        _queue.RemoveRange(1, Math.Min(amount - 1, _queue.Count));
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

            private async Task StartQueueThread()
            {
                if (_queueTask == null || _queueTask.IsCompleted)
                {
                    Log($"{_guildId}: Starting new queue task");
                    _queueTask = ProcessQueue();
                }

                await Task.CompletedTask;
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
                var userChannel = (_context.User as IGuildUser)?.VoiceChannel;

                Song currentSong = _queue[0];

                if (userChannel == null)
                {
                    return;
                }

                if (_audioClient == null || (_audioClient != null && (_audioClient.ConnectionState == ConnectionState.Disconnected || _audioClient.ConnectionState == ConnectionState.Disconnecting)))
                {
                    _audioClient = await userChannel.ConnectAsync();
                }

                currentSong.AudioStream = await _mediaDl.StreamAudio(currentSong, _tokenSource.Token);
                
                if (currentSong.AudioStream == null)
                {
                    return;
                }

                IUserMessage nowPlayingMessage = await SendNowPlayingMessage(currentSong);

                int playCount = 0;

                using (var ffmpegStream = await StartFFMPEG(currentSong.AudioStream))
                using (var discordStream = _audioClient!.CreatePCMStream(AudioApplication.Mixed))
                {
                    while ((!_tokenSource.IsCancellationRequested && _songAction == SongAction.Repeat) || (_songAction != SongAction.Repeat && playCount == 0) || (_songAction == SongAction.Resume && playCount == 0))
                    {
                        playCount++;

                        if (_songAction == SongAction.Repeat || _songAction == SongAction.Resume)
                        {
                            ffmpegStream.Seek(0, SeekOrigin.Begin);
                        }

                        try
                        {
                            try
                            {
                                await ffmpegStream.CopyToAsync(discordStream, _tokenSource.Token);
                            }
                            finally
                            {
                                await discordStream.FlushAsync(_tokenSource.Token);
                            }
                        } 
                        catch (OperationCanceledException) { }

                        if (_songAction == SongAction.Pause)
                        {
                            _tokenSource = new();
                            while (_songAction != SongAction.Resume)
                            {
                                Thread.Sleep(100);
                            }

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
                return await _context.Channel.SendMessageAsync(embed: builder.Build());
            }

            private async Task<MemoryStream> StartFFMPEG(Stream audioStream)
            {
                MemoryStream ms = new MemoryStream();

                await Cli.Wrap("ffmpeg")
                    .WithArguments(" -hide_banner -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1")
                    .WithStandardInputPipe(PipeSource.FromStream(audioStream))
                    .WithStandardOutputPipe(PipeTarget.ToStream(ms))
                    .ExecuteAsync();

                ms.Seek(0, SeekOrigin.Begin);
                return ms;
            }

            private void Log(string message)
            {
                Console.WriteLine($"{_guildId}: {message}");
            }
        }
        
    }
}
