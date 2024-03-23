using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Core.Models.Bandcamp
{
    internal class BandcampFile
    {
        [JsonProperty("mp3-128")]
        public string DownloadLink { get; set; } = "";
    }
}
