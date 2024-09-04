using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicBot.Core.Data
{
    public class FalAIClient
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public FalAIClient(IConfiguration config, IHttpClientFactory httpFactory)
        {
            _apiKey = config["FalAI_APIKey"]!.ToString();
            _httpClient = httpFactory.CreateClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Key {_apiKey}");
        }

        public async Task<GenerateImageResponse?> GenerateImage(string prompt, string? model = null)
        {
            model ??= "https://fal.run/fal-ai/flux/schnell";

            var content = new StringContent(JsonConvert.SerializeObject(new { prompt, enable_safety_checker = false }), Encoding.UTF8, "application/json");

            var request = await _httpClient.PostAsync(model, content);

            request.EnsureSuccessStatusCode();

            return JsonConvert.DeserializeObject<GenerateImageResponse>(await request.Content.ReadAsStringAsync());
        }
    }

    public class GenerateImageResponse
    {
        [JsonProperty("images")]
        public List<FalAIImage> Images { get; set; } = new List<FalAIImage>();

        [JsonProperty("has_nsfw_concepts")]
        public List<bool> IsNSFW { get; set; } = new List<bool>();
    }

    public class FalAIImage
    {
        [JsonProperty("url")]
        public required string Url { get; set; }

        [JsonProperty("content_type")]
        public required string ContentType { get; set; }
    }
}
