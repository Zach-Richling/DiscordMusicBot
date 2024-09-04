using Discord.Interactions;
using DiscordMusicBot.Core.Data;
using DiscordMusicBot.Core.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Client.InteractionHandlers
{
    public class AiImageHandler : InteractionModuleBase
    {
        private readonly AiImageModule _imageModule;
        private readonly BaseFunctions _common;

        public AiImageHandler(AiImageModule imageModule, BaseFunctions common)
        {
            _imageModule = imageModule;
            _common = common;
        }

        [SlashCommand("image", "Generate an AI Image.", runMode: RunMode.Async)]
        public async Task GenerateImageAsync(string prompt, string? falAiURL = null)
        {
            await DeferAsync();

            try
            {
                var image = await _imageModule.GenerateImage(Context, prompt, falAiURL);

                var nsfwMessage = image.IsNSFW ? "(NSFW Naughty Naughty)" : "";

                var message = _common.InitializeEmbedBuilder()
                    .WithDescription($"Prompt: {prompt} {nsfwMessage}")
                    .WithImageUrl(image.Url);

                await FollowupAsync(embed: message.Build());
            } 
            catch (Exception ex) 
            {
                var message = _common.InitializeEmbedBuilder().WithDescription(ex.Message);
                await FollowupAsync(embed: message.Build());
            }
        }
    }
}
