using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Core.Models.Apple
{
    internal class AppleAlbum
    {
        public string Name { get; set; } = "";
        public List<AppleTrack> Tracks { get; set; } = new List<AppleTrack>();
        [JsonProperty("byArtist")]
        public List<AppleArtist> Artists { get; set; } = new List<AppleArtist>();
    }
}
