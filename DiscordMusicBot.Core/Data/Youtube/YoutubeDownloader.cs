using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using Microsoft.Extensions.Configuration;
using DiscordMusicBot.Core.Models;
using Discord;

namespace DiscordMusicBot.Core.Data.Youtube
{
    public class YoutubeDownloader
    {
        private readonly YoutubeClient _client;

        public YoutubeDownloader()
        {
            _client = new YoutubeClient();
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

        public async Task<Stream> StreamAudio(Song song, CancellationToken cancellationToken)
        {
            var streamManifest = await _client.Videos.Streams.GetManifestAsync(song.Url);
            var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            return await _client.Videos.Streams.GetAsync(audioStreamInfo, cancellationToken);
        }

        private async Task<Song> CreateSong(string url)
        {
            var video = await _client.Videos.GetAsync(url);
            return new Song() { Url = video.Url, Name = video.Title, Length = video.Duration ?? default };
        }

        private async IAsyncEnumerable<Song> ParsePlaylist(string url)
        {
            await foreach (var video in _client.Playlists.GetVideosAsync(url))
            {
                yield return new Song() { Url = video.Url, Name = video.Title, Length = video.Duration ?? default };
            }
        }
    }
}
