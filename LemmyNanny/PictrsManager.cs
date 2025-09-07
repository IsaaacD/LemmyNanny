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

        public async Task<IEnumerable<byte[]>?> GetImageBytes(string url)
        {
            IEnumerable<byte[]>? results = null;
            if (string.IsNullOrEmpty(url))
            {
                return results;
            }
            try
            {
                results = url.Contains("/pictrs/") ? new[] { await _httpClient.GetByteArrayAsync(url) } : null;

                if (results != null)
                {
                    foreach (var consoleImage in results.Select(bytes => new CanvasImage(bytes)))
                    {
                        consoleImage.MaxWidth = 40;
                        AnsiConsole.Write(consoleImage);
                    }
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
