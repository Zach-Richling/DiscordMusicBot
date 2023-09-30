using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Core.Models
{
    public class Song
    {
        public string Url { get; set; } = "";
        public string Name { get; set; } = "";
        public TimeSpan Length { get; set; }
        public Stream? AudioStream { get; set; }
    }
}
