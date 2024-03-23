using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Core.Models.Bandcamp
{
    internal class BandcampTrack
    {
        [JsonProperty("file")]
        public BandcampFile File { get; set; } = new BandcampFile();
        public string Title { get; set; } = "";
        public float Duration { get; set; }
    }
}
