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

        public bool Downloading { get; set; }
        public bool Downloaded { get; set; }
        public string FilePath { get; set; } = "";
        public Task? Download { get; set; } = null;
        public CancellationTokenSource CancellationTokenSource { get; set; } = new();
    }
}
