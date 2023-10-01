using SoundCloudExplode;
using SoundCloudExplode.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace DiscordMusicBot.Core.Extensions
{
    public static class SoundCloudExplodeExtensions
    {
        public static HttpClient _httpClient = new HttpClient();
        public static async Task<Stream> GetStreamAsync(this SoundCloudClient client, string url, CancellationToken cancellationToken)
        {
            var downloadURL = await client.Tracks.GetDownloadUrlAsync(url);

            if (downloadURL == null)
            {
                throw new SoundcloudExplodeException("Could not get download url.");
            }

            var request = new HttpRequestMessage(HttpMethod.Get, downloadURL);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Response status code does not indicate success: {(int)response.StatusCode} ({response.StatusCode}).");
            }

            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }
    }
}
