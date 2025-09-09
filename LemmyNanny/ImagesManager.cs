using LemmyNanny.Interfaces;
using SixLabors.ImageSharp;
using Spectre.Console;
using System;
using System.Text.RegularExpressions;
using System.Threading;

namespace LemmyNanny
{
    public class ImagesManager : IImagesManager
    {
        public static string CLIENT_NAME = "PictrsClient";
        private readonly HttpClient _httpClient;

        public ImagesManager(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient(CLIENT_NAME);
        }

        public async Task<PromptContent> GetImageBytes(PromptContent content, CancellationToken token)
        {
            foreach (Match imageUrl in content.ImageMatches)
            {
                try
                {
                    var results = await _httpClient.GetByteArrayAsync(imageUrl.Value, token);
                    content.ImageBytes.Add(results);
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
                    AnsiConsole.MarkupInterpolated($"[red]*** post.Post.Url={imageUrl}. Cannot process UnknownImageFormatException. Likely type failure. ***[/]");
                    AnsiConsole.WriteLine("");
                }
                catch (Exception e)
                {
                    AnsiConsole.WriteLine("");
                    AnsiConsole.MarkupInterpolated($"{DateTime.Now}:[red]*** Failed {e.GetType()} - {e.Message}***[/]");
                    AnsiConsole.WriteLine("");
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
