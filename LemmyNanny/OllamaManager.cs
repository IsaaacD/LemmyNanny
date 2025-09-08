using LemmyNanny.Interfaces;
using OllamaSharp;
using Spectre.Console;
using System.Text;

namespace LemmyNanny
{
    public class OllamaManager : IOllamaManager
    {
        public static string CLIENT_NAME = "OllamaClient";

        private readonly string _prompt;
        private readonly IOllamaApiClient _ollamaApiClient;
        private string _fullPrompt => $"{_prompt}\r\nPlease output only 'Yes' if violation occurred or 'No' if the content is safe. After 'Yes', expand on what the post is about and violations that occurred.";

        public OllamaManager(IOllamaApiClient ollamaApiClient, string prompt)
        {
            _prompt = prompt;
            _ollamaApiClient = ollamaApiClient;
        }

        public async Task<PromptContent> CheckContent(PromptContent content, CancellationToken cancellation = default)
        {
            try
            {
                var chat = new Chat(_ollamaApiClient, _fullPrompt);
                var chatResults = chat.SendAsync(content.Content!, content.ImageBytes, cancellation);
             
                var sb = new StringBuilder();
                await foreach (var chatResult in chatResults)
                {
                    sb.Append(chatResult);
                    AnsiConsole.MarkupInterpolated($"[yellow]{chatResult}[/]");
                }
                AnsiConsole.WriteLine("");
                content.Result = sb.ToString();
            }
            catch (Exception)
            {
                AnsiConsole.WriteLine($"{DateTime.Now}: Issue with prompt {content.Id}");
                content.Failed = true;
            }

            return content;
        }

    }
}
