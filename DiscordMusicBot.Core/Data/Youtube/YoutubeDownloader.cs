using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using Microsoft.Extensions.Configuration;
using DiscordMusicBot.Core.Models;

namespace DiscordMusicBot.Core.Data.Youtube
{
    public class YoutubeDownloader
    {
        private readonly YoutubeClient _client;
        private readonly IConfiguration _config;
        public YoutubeDownloader(IConfiguration config)
        {
            _client = new YoutubeClient();
            _config = config;
        }

        public async Task<string> DownloadAudio(string url)
        {
            try
            {
                var streamManifest = await _client.Videos.Streams.GetManifestAsync(url);
                var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                string filePath = Path.Combine(_config["SongDir"], Guid.NewGuid().ToString() + ".mp3");
                await _client.Videos.Streams.DownloadAsync(audioStreamInfo, filePath);
                return filePath;
            } 
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }

        public async Task<Song> CreateSong(string url)
        {
            var video = _client.Videos.GetAsync(url);
            return new Song() { Url = video.Result.Url, Name = video.Result.Title, Length = video.Result.Duration ?? TimeSpan.MinValue };
        }

        public async IAsyncEnumerable<Song> ParsePlaylist(string url)
        {
            await foreach (var video in _client.Playlists.GetVideosAsync(url))
            {
                yield return new Song() { Url = video.Url, Name = video.Title, Length = video.Duration ?? TimeSpan.MinValue };
            }
        }
    }
}
