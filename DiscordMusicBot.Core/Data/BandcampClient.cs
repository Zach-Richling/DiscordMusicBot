using DiscordMusicBot.Core.Models.Bandcamp;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Core.Data
{
    internal class BandcampClient
    {
        private readonly HttpClient _httpClient = new HttpClient();

        private static readonly string albumJsonStart = "data-tralbum=\"{";
        private static readonly string albumJsonStop = "}\"";

        public async Task<BandcampAlbum> GetAlbumAsync(string url)
        {
            //Get HTML of bandcamp page
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();

            //Parse JSON of album
            var albumJsonWorking = html.Substring(html.IndexOf(albumJsonStart) + albumJsonStart.Length - 1);
            var albumJson = albumJsonWorking.Substring(0, albumJsonWorking.IndexOf(albumJsonStop) + 1);
            albumJson = WebUtility.HtmlDecode(albumJson);

            var album = JsonConvert.DeserializeObject<BandcampAlbum>(albumJson);

            if (album == null)
                throw new Exception("Could not parse bandcamp album");

            //Remove any tracks who don't have a download link
            album.Tracks = album.Tracks.Where(x => !string.IsNullOrEmpty(x.File?.DownloadLink)).ToList();

            return album;
        }

        public async Task<Stream> GetStreamAsync(string url, CancellationToken cancellationToken)
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }
    }
}
