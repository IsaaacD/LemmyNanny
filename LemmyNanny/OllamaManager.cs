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
        private string _fullPrompt => $"{_prompt}{Environment.NewLine}The first word you ouput will be 'Yes' or 'No'. Please output only 'Yes' if community violation occurred or only 'No', then guide based on this given prompt."; //if the content is safe. After 'Yes', expand on what the post is about and violations that occurred.";

        public OllamaManager(IOllamaApiClient ollamaApiClient, string prompt)
        {
            _prompt = prompt;
            _ollamaApiClient = ollamaApiClient;
        }

        public async Task<PromptResponse> CheckContent(PromptContent content, CancellationToken cancellation = default)
        {
            var promptResponse = new PromptResponse();
            try
            {
                var chat = new Chat(_ollamaApiClient, _fullPrompt);
                var chatResults = chat.SendAsync(content.Content!, content.ImageBytes, cancellation);
             
                var sb = new StringBuilder();
                AnsiConsole.WriteLine("");
                await foreach (var chatResult in chatResults)
                {
                    sb.Append(chatResult);
                    AnsiConsole.MarkupInterpolated($"[yellow]{chatResult}[/]");
                }
                AnsiConsole.WriteLine("");
                promptResponse.Result = sb.ToString();
                promptResponse.ImagesProcessed = content.ImageBytes.Count;
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteLine($"{DateTime.Now}: Issue with prompt {content.Id}");
                promptResponse.Failed = true;
                promptResponse.Result = "Failed to check";
            }

            content.PromptResponse = promptResponse;

            return promptResponse;
        }

    }
}
