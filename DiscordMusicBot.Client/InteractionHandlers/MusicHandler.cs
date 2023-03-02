using Discord.Interactions;
using DiscordMusicBot.Core.Data.Youtube;
using DiscordMusicBot.Core.Modules;
using Microsoft.Extensions.Configuration;

namespace DiscordMusicBot.Client.InteractionHandlers
{
    public class MusicHandler : InteractionModuleBase
    {
        private readonly MusicModule _musicModule;
        private readonly YoutubeDownloader _youtubeDl;

        public MusicHandler(MusicModule musicModule, YoutubeDownloader youtubeDl)
        {
            _musicModule = musicModule;
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
            try
            {
                var songs = await _youtubeDl.ProcessURL(url);
                _musicModule.Play(Context, songs, false);

                string result = "";
                if (songs.Count == 1)
                {
                    result = $"{songs[0].Name} added.";
                } 
                else
                {
                    result = $"{songs.Count} songs added.";
                }

                await FollowupAsync(result);
            } 
            catch (Exception)
            {
                await FollowupAsync("Error when adding songs. Was that a youtube link?");
            }
        }

        [SlashCommand("playtop", "Play a song at the top of the queue.", runMode: RunMode.Async)]
        public async Task PlayTopAsync(string url)
        {
            await DeferAsync();
            try
            {
                var songs = await _youtubeDl.ProcessURL(url);
                _musicModule.Play(Context, songs, true);

                string result = "";
                if (songs.Count == 1)
                {
                    result = $"{songs[0].Name} added.";
                }
                else
                {
                    result = $"{songs.Count} songs added.";
                }

                await FollowupAsync(result);
            } 
            catch (Exception)
            {
                await FollowupAsync("Error when adding songs. Was that a youtube link?");
            }
        }

        [SlashCommand("skip", "Skip a song.", runMode: RunMode.Async)]
        public async Task Skip()
        {
            _musicModule.Skip(Context);
            await RespondAsync("Song Skipped");
        }

        [SlashCommand("queue", "View the current queue.")]
        public async Task QueueAsync()
        {
            await DeferAsync();
            var songs = _musicModule.Queue(Context);
            await FollowupAsync(string.Join(", ", songs.Select(x => x.Name)));
        }
    }
}
