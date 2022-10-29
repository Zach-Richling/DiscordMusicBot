using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace DiscordMusicBot.Core.Data.Youtube
{
    public class YoutubeDownloader
    {
        private readonly YoutubeClient _client;
        public YoutubeDownloader()
        {
            _client = new YoutubeClient();
        }

        public async Task<string> DownloadAudio(string url)
        {
            try
            {
                var streamManifest = await _client.Videos.Streams.GetManifestAsync(url);
                var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                string filePath = Path.Combine("C:\\temp", Guid.NewGuid().ToString() + ".mp3");
                await _client.Videos.Streams.DownloadAsync(audioStreamInfo, filePath);
                return filePath;
            } 
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }

        public async IAsyncEnumerable<string> ParsePlaylist(string url)
        {
            var videos = _client.Playlists.GetVideosAsync(url);
            await foreach (var video in videos)
            {
                yield return video.Url;
            }
        }
    }
}
