using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DiscordMusicBot.Core.Models.Apple
{
    internal class AppleTrack
    {
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
        [JsonProperty("duration")]
        public string DurationString { get; set; } = "";
        [JsonIgnore]
        public TimeSpan Duration => XmlConvert.ToTimeSpan(DurationString);
    }
}
