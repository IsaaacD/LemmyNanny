using LemmyNanny.Interfaces;
using SixLabors.ImageSharp;
using Spectre.Console;
using System.Text.RegularExpressions;

namespace LemmyNanny
{
    public class ImagesManager : IImagesManager
    {
        public static string CLIENT_NAME = "ImagesClient";
        private readonly HttpClient _httpClient;

        public ImagesManager(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient(CLIENT_NAME);

            // image post not displaying https://lemmy.world/post/35674143

            // https://quokk.au/post/260327
            //https://lemmy.ml/post/35970324
            // https://europe.pub/post/4381176

            // another issue "Failed to check"

            // this one worked but no body https://lemmy.world/post/35686878
            // this one worked and full body https://programming.dev/post/37153435

            //https://lemmy.dbzer0.com/post/53169987 checked properly and flagged it, unsure if because of image
        }

        public async Task<PromptContent> GetImageBytes(PromptContent content, CancellationToken token)
        {
            foreach (Match imageUrl in content.ImageMatches)
            {
                var results = await GetImageBytes(imageUrl.Value, token);

                if (results != null)
                {
                    content.ImageBytes.Add(results);
                }
            }

            return content;
        }

        public async Task<byte[]?> GetImageBytes(string url, CancellationToken token = default)
        {
            byte[]? results = null;

            if (url == null)
            {
                return results;
            }
            try
            {
                results = await _httpClient.GetByteArrayAsync(url, token);
                if (results != null)
                {
                    AnsiConsole.WriteLine("");
                    AnsiConsole.Write(new CanvasImage(results) { MaxWidth = 40 });
                    AnsiConsole.WriteLine("");
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
                AnsiConsole.WriteLine("");
                AnsiConsole.MarkupInterpolated($"{DateTime.Now}:[red]*** Failed {e.GetType()} - {e.Message}***[/]");
                AnsiConsole.WriteLine("");
            }
            return results;
        }
    }
}
