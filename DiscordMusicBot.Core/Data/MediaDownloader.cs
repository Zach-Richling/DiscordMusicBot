using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using DiscordMusicBot.Core.Models;
using DiscordMusicBot.Core.Enums;
using SoundCloudExplode;
using DiscordMusicBot.Core.Extensions;
using SpotifyExplode;
using System.Text.RegularExpressions;

namespace DiscordMusicBot.Core.Data
{
    public class MediaDownloader
    {
        private readonly YoutubeClient _youtubeClient;
        private readonly SoundCloudClient _soundcloudClient;
        private readonly SpotifyClient _spotifyClient;
        private readonly BandcampClient _bandcampClient;

        private static readonly Regex youtubeLongListRegex = new Regex(@"https:\/\/www(?:\.music)?\.youtube\.com\/watch\?v=.+&list=.+");
        private static readonly Regex youtubeShortListRegex = new Regex(@"https:\/\/youtu\.be\/.+&list.+");
        private static readonly Regex youtubeLongVideoRegex = new Regex(@"https:\/\/www(?:\.music)?\.youtube\.com\/watch\?v=.+");
        private static readonly Regex youtubeShortVideoRegex = new Regex(@"https:\/\/youtu\.be\/.+");

        private static readonly Regex soundCloudPlaylistRegex = new Regex(@"https:\/\/soundcloud\.com\/.+\/sets\/.+");
        private static readonly Regex soundCloudSongRegex = new Regex(@"https:\/\/soundcloud\.com\/.+");

        private static readonly Regex spotifyPlaylistRegex = new Regex(@"https:\/\/open\.spotify\.com\/playlist\/.+");
        private static readonly Regex spotifyAlbumRegex = new Regex(@"https:\/\/open\.spotify\.com\/album\/.+");
        private static readonly Regex spotifyTrackRegex = new Regex(@"https:\/\/open\.spotify\.com\/track\/.+");

        private static readonly Regex bandcampRegex = new Regex(@"(?<artist>https:\/\/.+\.bandcamp\.com\/(?:track|album).+)");

        public MediaDownloader()
        {
            _youtubeClient = new YoutubeClient();
            _soundcloudClient = new SoundCloudClient();
            _spotifyClient = new SpotifyClient();
            _bandcampClient = new BandcampClient();
        }

        public async Task<List<Song>> ProcessURL(string url)
        {
            if (IsYoutubePlaylist(url))
            {
                return await ParseYoutubePlaylist(url).ToListAsync();
            }
            else if (IsYoutubeVideo(url))
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
            else if (IsSpotifyTrack(url))
            {
                return new List<Song>() { await CreateSpotifySong(url) };
            } 
            else if (IsBandcamp(url))
            {
                return await ParseBandcamp(url);
            } 
            else if (!IsURL(url))
            {
                return new List<Song>() { await SearchSong(url) };
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
                var youtubeVideo = await _youtubeClient.Search.GetVideosAsync($"{song.Name} {song.Artist}", cancellationToken).FirstAsync(cancellationToken);
                return await GetYoutubeStream(youtubeVideo.Url, cancellationToken);
            }
            else if (song.Source == SongSource.Bandcamp)
            {
                return await _bandcampClient.GetTrackStreamAsync(song.Url, cancellationToken);
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

        private async Task<Song> SearchSong(string searchExpression)
        {
            var video = await _youtubeClient.Search.GetVideosAsync(searchExpression).FirstAsync();
            return new Song() { Url = video.Url, Name = video.Title, Length = video.Duration ?? default, Source = SongSource.Youtube };
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

        private async Task<List<Song>> ParseBandcamp(string url)
        {
            var album = await _bandcampClient.GetAlbumAsync(url);

            return album.Tracks.Select(track => new Song() 
            { 
                Url = track.File.DownloadLink,
                Name = track.Title,
                Length = TimeSpan.FromSeconds(track.Duration),
                Source = SongSource.Bandcamp
            }).ToList();
        }

        private bool IsYoutubeVideo(string url)
        {
            if (youtubeLongVideoRegex.IsMatch(url) || youtubeShortVideoRegex.IsMatch(url))
            {
                return true;
            }

            return false;
        }

        private bool IsYoutubePlaylist(string url)
        {
            if (youtubeLongListRegex.IsMatch(url) || youtubeShortListRegex.IsMatch(url))
            {
                return true;
            }

            return false;
        }

        private bool IsSoundCloud(string url)
        {
            if (soundCloudSongRegex.IsMatch(url))
            {
                return true;
            }

            return false;
        }

        private bool IsSoundCloudPlaylist(string url)
        {
            if (soundCloudPlaylistRegex.IsMatch(url))
            {
                return true;
            }

            return false;
        }

        private bool IsSpotifyTrack(string url)
        {
            if (spotifyTrackRegex.IsMatch(url))
            {
                return true;
            }

            return false;
        }

        private bool IsSpotifyPlaylist(string url)
        {
            if (spotifyPlaylistRegex.IsMatch(url))
            {
                return true;
            }

            return false;
        }

        private bool IsSpotifyAlbum(string url)
        {
            if (spotifyAlbumRegex.IsMatch(url))
            {
                return true;
            }

            return false;
        }

        private bool IsBandcamp(string url)
        {
            return bandcampRegex.IsMatch(url);
        }

        private bool IsURL(string url)
        {
            if (Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
            {
                var uri = new Uri(url);
                return uri.Scheme == Uri.UriSchemeHttps;
            }

            return false;
        }
    }
}
