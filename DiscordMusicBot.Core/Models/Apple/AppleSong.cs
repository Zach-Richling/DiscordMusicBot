using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Core.Models.Apple
{
    internal class AppleSong
    {
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
        public AppleAudio Audio { get; set; } = new AppleAudio();
        
    }
}
