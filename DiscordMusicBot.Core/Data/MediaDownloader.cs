using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using DiscordMusicBot.Core.Models;
using DiscordMusicBot.Core.Enums;
using SoundCloudExplode;
using DiscordMusicBot.Core.Extensions;
using SpotifyExplode;
using System.Text.RegularExpressions;
using CliWrap;
using Discord.Audio;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Diagnostics;

namespace DiscordMusicBot.Core.Data
{
    public class MediaDownloader
    {
        private readonly YoutubeClient _youtubeClient;
        private readonly SoundCloudClient _soundcloudClient;
        private readonly SpotifyClient _spotifyClient;
        private readonly BandcampClient _bandcampClient;
        private readonly AppleClient _appleClient;

        private static readonly Regex youtubeLongListRegex = new Regex(@"(?:https:\/\/)?(?:www\.)?(?:music\.)?youtube\.com\/watch\?v=.+&list=.+");
        private static readonly Regex youtubeShortListRegex = new Regex(@"(?:https:\/\/)?youtu\.be\/.+&list.+");
        private static readonly Regex youtubeLongPlaylistRegex = new Regex(@"(?:https:\/\/)?(?:www\.)?(?:music\.)?youtube\.com\/playlist\?list=.+");
        private static readonly Regex youtubeLongVideoRegex = new Regex(@"(?:https:\/\/)?(?:www\.)?(?:music\.)?youtube\.com\/watch\?v=.+");
        private static readonly Regex youtubeShortVideoRegex = new Regex(@"(?:https:\/\/)?youtu\.be\/.+");

        private static readonly Regex soundCloudPlaylistRegex = new Regex(@"(?:https:\/\/)?soundcloud\.com\/.+\/sets\/.+");
        private static readonly Regex soundCloudSongRegex = new Regex(@"(?:https:\/\/)?soundcloud\.com\/.+");

        private static readonly Regex spotifyPlaylistRegex = new Regex(@"(?:https:\/\/)?open\.spotify\.com\/playlist\/.+");
        private static readonly Regex spotifyAlbumRegex = new Regex(@"(?:https:\/\/)?open\.spotify\.com\/album\/.+");
        private static readonly Regex spotifyTrackRegex = new Regex(@"(?:https:\/\/)?open\.spotify\.com\/track\/.+");

        private static readonly Regex bandcampRegex = new Regex(@"(?:https:\/\/)?.+\.bandcamp\.com\/(?:track|album).+");

        private static readonly Regex appleAlbumRegex = new Regex(@"(?:https:\/\/)?music\.apple\.com\/.+\/album\/.+");
        private static readonly Regex appleSongRegex = new Regex(@"(?:https:\/\/)?music\.apple\.com\/.+\/song\/.+");

        private readonly IConfiguration _config;

        public MediaDownloader(IConfiguration config)
        {
            _youtubeClient = new YoutubeClient();
            _soundcloudClient = new SoundCloudClient(config["SoundCloudClientId"]!.ToString());
            _spotifyClient = new SpotifyClient();
            _bandcampClient = new BandcampClient();
            _appleClient = new AppleClient();
            _config = config;
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
            else if (IsAppleAlbum(url))
            {
                return await ParseAppleAlbum(url);
            }
            else if (IsAppleSong(url))
            {
                return new List<Song>() { await CreateAppleSong(url) };
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
            return song.Source switch
            {
                SongSource.Youtube => await GetYoutubeStream(song.Url, cancellationToken),
                SongSource.SoundCloud => await _soundcloudClient.GetStreamAsync(song.Url, cancellationToken),
                SongSource.Spotify or SongSource.Apple => await SearchForStreamAsync(song, cancellationToken),
                SongSource.Bandcamp => await _bandcampClient.GetStreamAsync(song.Url, cancellationToken),
                _ => throw new NotImplementedException()
            };
        }

        private async Task<Stream> GetYoutubeStream(string url, CancellationToken cancellationToken)
        {
            var audioDir = _config["AudioDirectory"]!.ToString();
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".tmp");

            await Process.Start(
                Path.Combine(audioDir, "yt-dlp.exe")
                ,$"\"{url}\" --no-playlist --format \"bestaudio[ext=m4a]\" -o \"{tempFile}\""
            ).WaitForExitAsync();

            return new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose);
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

        private async Task<Stream> SearchForStreamAsync(Song song, CancellationToken cancellationToken)
        {
            var youtubeVideo = await _youtubeClient.Search.GetVideosAsync($"{song.Name} {song.Artist}", cancellationToken).FirstAsync(cancellationToken);
            return await GetYoutubeStream(youtubeVideo.Url, cancellationToken);
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

            return tracks.Where(x => x != null).Select(track => new Song()
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

        private async Task<List<Song>> ParseAppleAlbum(string url)
        {
            var album = await _appleClient.GetAlbumAsync(url);

            return album.Tracks.Select(track => new Song()
            {
                Url = track.Url,
                Name = track.Name,
                Length = track.Duration,
                Artist = album.Artists.FirstOrDefault()?.Name ?? "",
                Source = SongSource.Apple
            }).ToList();
        }

        private async Task<Song> CreateAppleSong(string url)
        {
            var song = await _appleClient.GetAppleSongAsync(url);

            return new Song() 
            { 
                Url = song.Url,
                Name = song.Name,
                Length = song.Audio.Duration,
                Artist = song.Audio.Artists.FirstOrDefault()?.Name ?? "",
                Source = SongSource.Apple
            };
        }

        private bool IsYoutubeVideo(string url)
        {
            return youtubeLongVideoRegex.IsMatch(url) || youtubeShortVideoRegex.IsMatch(url);
        }

        private bool IsYoutubePlaylist(string url)
        {
            return youtubeLongListRegex.IsMatch(url) || youtubeShortListRegex.IsMatch(url) || youtubeLongPlaylistRegex.IsMatch(url);
        }

        private bool IsSoundCloud(string url)
        {
            return soundCloudSongRegex.IsMatch(url);
        }

        private bool IsSoundCloudPlaylist(string url)
        {
            return soundCloudPlaylistRegex.IsMatch(url);
        }

        private bool IsSpotifyTrack(string url)
        {
            return spotifyTrackRegex.IsMatch(url);
        }

        private bool IsSpotifyPlaylist(string url)
        {
            return spotifyPlaylistRegex.IsMatch(url);
        }

        private bool IsSpotifyAlbum(string url)
        {
            return spotifyAlbumRegex.IsMatch(url);
        }

        private bool IsBandcamp(string url)
        {
            return bandcampRegex.IsMatch(url);
        }

        private bool IsAppleAlbum(string url)
        {
            return appleAlbumRegex.IsMatch(url);
        }

        private bool IsAppleSong(string url)
        {
            return appleSongRegex.IsMatch(url);
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
