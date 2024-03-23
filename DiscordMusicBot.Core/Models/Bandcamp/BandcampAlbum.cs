using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Core.Models.Bandcamp
{
    internal class BandcampAlbum
    {
        [JsonProperty("trackinfo")]
        public List<BandcampTrack> Tracks { get; set; } = new List<BandcampTrack>();
    }
}
