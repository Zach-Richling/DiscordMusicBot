using DiscordMusicBot.Core.Enums;
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
        public string Artist { get; set; } = "";
        public TimeSpan Length { get; set; }
        public SongSource Source { get; set; }
    }
}
