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

        public async Task<List<Song>> ProcessURL(string url)
        {
            if (url.StartsWith("https://www.youtube.com/playlist?") || url.Contains("&list"))
            {
                return await ParsePlaylist(url).ToListAsync();
            }
            else
            {
                return new List<Song>() { await CreateSong(url) };
            }
        }

        public async Task DownloadAudio(Song song, CancellationToken cancellationToken, IProgress<double>? progress = null)
        {
            var streamManifest = await _client.Videos.Streams.GetManifestAsync(song.Url);
            var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            string filePath = Path.Combine(_config["SongDir"]!, $"{Guid.NewGuid()}.{audioStreamInfo.Container}");
            song.Downloading = true;
            song.FilePath = filePath;
            await _client.Videos.Streams.DownloadAsync(audioStreamInfo, filePath, progress, cancellationToken);
            song.Downloaded = false;
            song.Downloaded = true;
        }

        private async Task<Song> CreateSong(string url)
        {
            var video = await _client.Videos.GetAsync(url);
            return new Song() { Url = video.Url, Name = video.Title, Length = video.Duration ?? TimeSpan.MinValue };
        }

        private async IAsyncEnumerable<Song> ParsePlaylist(string url)
        {
            await foreach (var video in _client.Playlists.GetVideosAsync(url))
            {
                yield return new Song() { Url = video.Url, Name = video.Title, Length = video.Duration ?? TimeSpan.MinValue };
            }
        }
    }
}
