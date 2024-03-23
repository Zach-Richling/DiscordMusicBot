using DiscordMusicBot.Core.Models.Apple;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace DiscordMusicBot.Core.Data
{
    internal class AppleClient
    {
        private readonly HttpClient _httpClient = new HttpClient();

        private static readonly string htmlAlbumId = "schema:music-album";
        private static readonly string htmlSongId = "schema:song";

        public async Task<AppleAlbum> GetAlbumAsync(string url)
        {
            var response = await _httpClient.GetAsync(url);

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(await response.Content.ReadAsStringAsync());

            var albumElement = htmlDoc.GetElementbyId(htmlAlbumId);

            if (albumElement == null)
                throw new Exception("Could not find proper html element.");

            var albumJson = albumElement.InnerText.Trim();
            var album = JsonConvert.DeserializeObject<AppleAlbum>(albumJson);

            if (album == null) 
                throw new Exception("Could not parse JSON.");

            return album;
        }

        public async Task<AppleSong> GetAppleSongAsync(string url)
        {
            var response = await _httpClient.GetAsync(url);

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(await response.Content.ReadAsStringAsync());

            var songElement = htmlDoc.GetElementbyId(htmlSongId);

            if (songElement == null) 
                throw new Exception("Could not find proper html element.");

            var songJson = songElement.InnerText.Trim();
            var song = JsonConvert.DeserializeObject<AppleSong>(songJson);

            if (song == null) 
                throw new Exception("Could not parse JSON.");

            return song;
        }
    }
}
