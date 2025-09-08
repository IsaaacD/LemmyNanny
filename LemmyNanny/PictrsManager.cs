using LemmyNanny.Interfaces;
using SixLabors.ImageSharp;
using Spectre.Console;

namespace LemmyNanny
{
    public class PictrsManager : IPictrsManager
    {
        public static string CLIENT_NAME = "PictrsClient";
        private readonly HttpClient _httpClient;

        public PictrsManager(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient(CLIENT_NAME);
        }

        public async Task<byte[]?> GetImageBytes(string url, CancellationToken token = default)
        {
            byte[]? results = null;
            if (string.IsNullOrEmpty(url))
            {
                return results;
            }
            try
            {
                results = await _httpClient.GetByteArrayAsync(url, token);

                if (results != null)
                {
                    AnsiConsole.Write(new CanvasImage(results) { MaxWidth = 40 });
                }
            }
            catch (UnknownImageFormatException)
            {
                AnsiConsole.WriteLine("");
                AnsiConsole.MarkupInterpolated($"[red]*** post.Post.Url={url}. Cannot process UnknownImageFormatException. Likely type failure. ***[/]");
                AnsiConsole.WriteLine("");
            }
            catch (Exception e)
            {
                AnsiConsole.MarkupInterpolated($"{DateTime.Now}:[red]*** Failed {e.GetType()} - {e.Message}***[/]");
            }

            return results;
        }
    }
}
