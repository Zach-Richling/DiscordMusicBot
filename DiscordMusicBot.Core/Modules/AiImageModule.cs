using Discord;
using DiscordMusicBot.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Core.Modules
{
    public class AiImageModule
    {
        private readonly FalAIClient _client;

        public AiImageModule(FalAIClient client)
        {
            _client = client;
        }

        public async Task<AiImage> GenerateImage(IInteractionContext context, string prompt, string? falAIURL = null)
        {
            var response = await _client.GenerateImage(prompt, falAIURL);

            if (response == null)
            {
                throw new Exception("Could not generate image.");
            }

            return new AiImage(response.Images[0].Url, response.IsNSFW[0]);
        }
    }

    public record AiImage(string Url, bool IsNSFW);
}
