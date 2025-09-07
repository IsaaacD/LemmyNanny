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
        private readonly Chat _chat;
        private string _fullPrompt => $"{_prompt}\r\nPlease output only 'Yes' if violation occurred or 'No' if the content is safe. After 'Yes' or 'No', expand on what the post is about and violations that occurred.";

        public OllamaManager(IOllamaApiClient ollamaApiClient, string prompt)
        {
            _prompt = prompt;
            _chat = new Chat(ollamaApiClient, _fullPrompt);
        }

        public async Task<PromptContent> CheckContent(PromptContent content, CancellationToken cancellation = default)
        {
            var chatResults = _chat.SendAsync(content.Content, content.ImageBytes, cancellation);
            var sb = new StringBuilder();
            await foreach (var chatResult in chatResults)
            {
                sb.Append(chatResult);
                AnsiConsole.MarkupInterpolated($"[yellow]{chatResult}[/]");
            }

            content.Result = sb.ToString();

            return content;
        }

    }
}
