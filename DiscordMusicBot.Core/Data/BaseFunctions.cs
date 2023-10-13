using Discord;
using DiscordMusicBot.Core.Enums;
using DiscordMusicBot.Core.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                _ => ""
            };

            string name = song.Source switch
            {
                SongSource.Youtube => song.Name,
                SongSource.SoundCloud => $"{song.Name} by {song.Artist}",
                SongSource.Spotify => $"{song.Name} by {song.Artist}",
                _ => ""
            };
            return $"{emoji} {name.Replace("*", "\\*")}";
        }
    }
}
