using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using DiscordMusicBot.Core.Models;
using DiscordMusicBot.Core.Enums;
using SoundCloudExplode;
using System;
using DiscordMusicBot.Core.Extensions;
using YoutubeExplode.Videos;

namespace DiscordMusicBot.Core.Data
{
    public class MediaDownloader
    {
        private readonly YoutubeClient _youtubeClient;
        private readonly SoundCloudClient _soundcloudClient;

        public MediaDownloader()
        {
            _youtubeClient = new YoutubeClient();
            _soundcloudClient = new SoundCloudClient();
        }

        public async Task<List<Song>> ProcessURL(string url)
        {
            if (url.Contains("youtube.com/playlist?") || url.Contains("youtube.com/") && url.Contains("&list"))
            {
                return await ParseYoutubePlaylist(url).ToListAsync();
            }
            else if (url.Contains("youtube.com/"))
            {
                return new List<Song>() { await CreateYoutubeSong(url) };
            }
            else if (url.Contains("soundcloud.com/playlist") || (url.Contains("soundcloud.com") && url.Contains("/sets/")))
            {
                return await ParseSoundCloudPlaylist(url).ToListAsync();
            }
            else if (url.Contains("soundcloud.com/"))
            {
                return new List<Song>() { await CreateSoundCloudSong(url) };
            }
            {
                throw new NotImplementedException("URL not supported type.");
            }
        }

        public async Task<Stream> StreamAudio(Song song, CancellationToken cancellationToken)
        {
            if (song.Source == SongSource.Youtube) 
            {
                var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(song.Url);
                var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                return await _youtubeClient.Videos.Streams.GetAsync(audioStreamInfo, cancellationToken);
            }
            else if (song.Source == SongSource.SoundCloud)
            {
                return await _soundcloudClient.GetStreamAsync(song.Url, cancellationToken);
            } 
            else
            {
                throw new NotImplementedException();
            }
        }

        private async Task<Song> CreateYoutubeSong(string url)
        {
            var video = await _youtubeClient.Videos.GetAsync(url);
            return new Song() { Url = video.Url, Name = video.Title, Length = video.Duration ?? default, Source = SongSource.Youtube };
        }

        private async IAsyncEnumerable<Song> ParseYoutubePlaylist(string url)
        {
            await foreach (var video in _youtubeClient.Playlists.GetVideosAsync(url))
            {
                yield return new Song() { Url = video.Url, Name = video.Title, Length = video.Duration ?? default, Source = SongSource.Youtube };
            }
        }

        private async Task<Song> CreateSoundCloudSong(string url)
        {
            var track = await _soundcloudClient.Tracks.GetAsync(url);
            return new Song() { Url = url, Name = track?.Title ?? "", Length = TimeSpan.FromMilliseconds(Convert.ToDouble(track?.Duration ?? 0)), Source = SongSource.SoundCloud };
        }

        private async IAsyncEnumerable<Song> ParseSoundCloudPlaylist(string url)
        {
            await foreach (var track in _soundcloudClient.Playlists.GetTracksAsync(url))
            {
                yield return new Song() { Url = track.PermalinkUrl?.ToString() ?? "", Name = track.Title ?? "", Length = TimeSpan.FromMilliseconds(Convert.ToDouble(track?.Duration ?? 0)), Source = SongSource.SoundCloud };
            }
        }
    }
}
