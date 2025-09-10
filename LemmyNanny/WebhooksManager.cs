using dotNETLemmy.API.Types;
using LemmyNanny.Interfaces;
using Spectre.Console;
using System.Net.Http.Json;

namespace LemmyNanny
{
    public class WebhooksManager : IWebhooksManager
    {
        public static string CLIENT_NAME = "WebhooksClient";
        private readonly HttpClient _httpClient;
        private readonly List<WebhookConfig> _urls;

        public int Posts { get; private set; }
        public int Comments { get; private set; }
        public int CommentsFlagged { get; private set; }
        public int PostsFlagged { get; private set; }

        public DateTime StartTime { get; private set; }
        public TimeSpan ElapsedTime => DateTime.Now - StartTime;
        public List<Processed> History { get; set; } = [];

        public WebhooksManager(IHttpClientFactory httpClientFactory, List<WebhookConfig> urls, DateTime datetime)
        {
            _httpClient = httpClientFactory.CreateClient(CLIENT_NAME);
            _urls = urls;
            StartTime = datetime;
        }

        public async Task SendToWebhooksAndUpdateStats(Processed processed)
        {
            switch (processed.ProcessedType)
            {
                case ProcessedType.NotSet:
                    AnsiConsole.WriteLine("NotSet processed sending to webhooks");
                    break;
                case ProcessedType.Comment:
                    Comments++;
                    CommentsFlagged += processed.IsReported ? 1 : 0;
                    AnsiConsole.WriteLine($"Processing Comment to Webhooks processed {Comments}, flagged {CommentsFlagged} of them.");
                    break;
                case ProcessedType.Post:
                    Posts++;
                    PostsFlagged += processed.IsReported ? 1 : 0;
                    AnsiConsole.WriteLine($"Processing Post to Webhooks processed {Posts}, flagged {PostsFlagged} of them.");
                    break;
                default:
                    break;
            }
            History.Add(processed);

            if (History.Count > 50)
            {
                History.RemoveAt(0);
            }

            try
            {
                if (_urls.Count != 0)
                {
                    foreach (var config in _urls)
                    {
                        var jsonContent = JsonContent.Create(processed);
                        jsonContent.Headers.Add("ClientSecret", config.Secret);
                        await _httpClient.PostAsync(config.Url,  jsonContent);
                        AnsiConsole.WriteLine($"Forwarded JsonContent to {config.Url} succesfully.");
                    }
                }
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLineInterpolated($"***[red]There was an issue with connection to one of the webhooks.***[/]");
            }
        }
    }
}
