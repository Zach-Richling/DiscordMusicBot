using Discord;
using DiscordMusicBot.Core.Enums;
using DiscordMusicBot.Core.Models;
using Microsoft.Extensions.Configuration;

namespace DiscordMusicBot.Core.Data
{
    public class BaseFunctions
    {
        private readonly IConfiguration _config;

        public BaseFunctions(IConfiguration config)
        {
            _config = config;
        }

        public EmbedBuilder InitializeEmbedBuilder()
        {
            return new EmbedBuilder().WithColor(Color.Orange);
        }

        public string NameWithEmoji(Song song)
        {
            string emoji = song.Source switch
            {
                SongSource.Youtube => _config["YoutubeEmoji"] ?? "",
                SongSource.SoundCloud => _config["SoundCloudEmoji"] ?? "",
                SongSource.Spotify => _config["SpotifyEmoji"] ?? "",
                SongSource.Bandcamp => _config["BandcampEmoji"] ?? "",
                SongSource.Apple => _config["AppleEmoji"] ?? "",
                _ => ""
            };

            string name = song.Source switch
            {
                SongSource.Youtube or SongSource.Bandcamp => song.Name,
                SongSource.SoundCloud or SongSource.Spotify or SongSource.Apple => $"{song.Name} by {song.Artist}",
                _ => song.Name
            };
            return $"{emoji} {name.Replace("*", "\\*")}";
        }
    }
}
