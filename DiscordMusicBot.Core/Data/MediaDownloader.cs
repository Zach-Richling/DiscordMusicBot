using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using DiscordMusicBot.Core.Models;
using DiscordMusicBot.Core.Enums;
using SoundCloudExplode;
using System;
using DiscordMusicBot.Core.Extensions;
using YoutubeExplode.Videos;
using Microsoft.Extensions.Configuration;
using SpotifyExplode;
using System.Threading;

namespace DiscordMusicBot.Core.Data
{
    public class MediaDownloader
    {
        private readonly YoutubeClient _youtubeClient;
        private readonly SoundCloudClient _soundcloudClient;
        private readonly SpotifyClient _spotifyClient;
        private readonly IConfiguration _appConfig;

        public MediaDownloader(IConfiguration appConfig)
        {
            _youtubeClient = new YoutubeClient();
            _soundcloudClient = new SoundCloudClient();
            _spotifyClient = new SpotifyClient();
            _appConfig = appConfig;
        }

        public async Task<List<Song>> ProcessURL(string url)
        {
            if (IsYoutubePlaylist(url))
            {
                return await ParseYoutubePlaylist(url).ToListAsync();
            }
            else if (IsYoutube(url))
            {
                return new List<Song>() { await CreateYoutubeSong(url) };
            }
            else if (IsSoundCloudPlaylist(url))
            {
                return await ParseSoundCloudPlaylist(url).ToListAsync();
            }
            else if (IsSoundCloud(url))
            {
                return new List<Song>() { await CreateSoundCloudSong(url) };
            }
            else if (IsSpotifyPlaylist(url))
            {
                return await ParseSpotifyPlaylist(url);
            }
            else if (IsSpotifyAlbum(url))
            {
                return await ParseSpotifyAlbum(url);
            }
            else if (IsSpotify(url))
            {
                return new List<Song>() { await CreateSpotifySong(url) };
            }
            else
            {
                throw new NotImplementedException("URL not supported type.");
            }
        }

        public async Task<Stream> StreamAudio(Song song, CancellationToken cancellationToken)
        {
            if (song.Source == SongSource.Youtube) 
            {
                return await GetYoutubeStream(song.Url, cancellationToken);
            }
            else if (song.Source == SongSource.SoundCloud)
            {
                return await _soundcloudClient.GetStreamAsync(song.Url, cancellationToken);
            }
            else if (song.Source == SongSource.Spotify)
            {
                var youtubeVideo = await _youtubeClient.Search.GetVideosAsync($"{song.Name} {song.Artist}", cancellationToken).FirstAsync();
                return await GetYoutubeStream(youtubeVideo.Url, cancellationToken);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private async Task<Stream> GetYoutubeStream(string url, CancellationToken cancellationToken)
        {
            var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(url);
            var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            return await _youtubeClient.Videos.Streams.GetAsync(audioStreamInfo, cancellationToken);
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

        private async Task<Song> CreateSpotifySong(string url)
        {
            var track = await _spotifyClient.Tracks.GetAsync(url);
            
            return new Song() 
            { 
                Url = track.Url,
                Name = track.Title,
                Artist = track.Artists.FirstOrDefault()?.Name ?? "",
                Length = TimeSpan.FromMilliseconds(Convert.ToDouble(track.DurationMs)),
                Source = SongSource.Spotify 
            };
        }

        private async Task<List<Song>> ParseSpotifyPlaylist(string url)
        {
            var tracks = await _spotifyClient.Playlists.GetTracksAsync(url);

            return tracks.Select(track => new Song()
            {
                Url = track.Url,
                Name = track.Title,
                Artist = track.Artists.FirstOrDefault()?.Name ?? "",
                Length = TimeSpan.FromMilliseconds(Convert.ToDouble(track.DurationMs)),
                Source = SongSource.Spotify
            }).ToList();
        }

        private async Task<List<Song>> ParseSpotifyAlbum(string url)
        {
            var tracks = await _spotifyClient.Albums.GetTracksAsync(url);

            return tracks.Select(track => new Song()
            {
                Url = track.Url,
                Name = track.Title,
                Artist = track.Artists.FirstOrDefault()?.Name ?? "",
                Length = TimeSpan.FromMilliseconds(Convert.ToDouble(track.DurationMs)),
                Source = SongSource.Spotify
            }).ToList();
        }

        private bool IsYoutube(string url)
        {
            if (url.Contains("youtube.com") || url.Contains("youtu.be"))
            {
                return true;
            }

            return false;
        }

        private bool IsYoutubePlaylist(string url)
        {
            if (IsYoutube(url))
            {
                if (url.Contains("&list"))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsSoundCloud(string url)
        {
            if (url.Contains("soundcloud.com"))
            {
                return true;
            }

            return false;
        }

        private bool IsSoundCloudPlaylist(string url)
        {
            if (IsSoundCloud(url))
            {
                if (url.Contains("/playlist") || url.Contains("/sets/"))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsSpotify(string url)
        {
            if (url.Contains("open.spotify"))
            {
                return true;
            }

            return false;
        }

        private bool IsSpotifyPlaylist(string url)
        {
            if (IsSpotify(url))
            {
                if (url.Contains("/playlist/"))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsSpotifyAlbum(string url)
        {
            if (IsSpotify(url))
            {
                if (url.Contains("/album/"))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
