using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DiscordMusicBot.Core.Models.Apple
{
    internal class AppleAudio
    {
        [JsonProperty("duration")]
        public string DurationString { get; set; } = "";
        public TimeSpan Duration  => XmlConvert.ToTimeSpan(DurationString);
        [JsonProperty("byArtist")]
        public List<AppleArtist> Artists { get; set; } = new List<AppleArtist>();
    }
}
