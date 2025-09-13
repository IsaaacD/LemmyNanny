using dotNETLemmy.API.Types;
using LemmyNanny.Interfaces;
using Spectre.Console;
using System.Diagnostics;
using System.Net.Http.Json;

namespace LemmyNanny
{
    public class WebhooksManager : IWebhooksManager
    {
        private Stopwatch _timer = new();
        private long _lastWaitTime = 0;

        public static string CLIENT_NAME = "WebhooksClient";

        private readonly HttpClient _httpClient;
        private readonly List<WebhookConfig> _urls;
        private readonly bool _readingMode;

        public int Posts { get; private set; }
        public int Comments { get; private set; }
        public int CommentsFlagged { get; private set; }
        public int PostsFlagged { get; private set; }

        public DateTime StartTime { get; private set; }
        public TimeSpan ElapsedTime => DateTime.UtcNow - StartTime;
        public List<Processed> History { get; set; } = [];

        public WebhooksManager(IHttpClientFactory httpClientFactory, List<WebhookConfig> urls, DateTime datetime, bool readingMode)
        {
            _httpClient = httpClientFactory.CreateClient(CLIENT_NAME);
            _urls = urls;
            StartTime = datetime;
            _readingMode = readingMode;
        }

        public async Task SendStartupStats(StartUpStats stats)
        {
            if (_urls.Count != 0)
            {
                foreach (var config in _urls)
                {
                    if (config.ShouldProcess)
                    {
                        try
                        {
                            var jsonContent = JsonContent.Create(stats);
                            jsonContent.Headers.Add("ClientSecret", config.Secret);
                            await _httpClient.PostAsync(config.StartupUrl, jsonContent);
                            AnsiConsole.WriteLine($"Forwarded JsonContent to {config.StartupUrl} succesfully.");
                            config.FailedTimes = 0;
                        }
                        catch (Exception)
                        {
                            AnsiConsole.MarkupLineInterpolated($"[red]***There was an issue sending to {config.StartupUrl} webhook. ***[/]");
                        }
                    }
                }
            }
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

            if (_timer.IsRunning)
            {
                var elapsed = _timer.ElapsedMilliseconds;
                if (elapsed < _lastWaitTime)
                { 
                    var waiting = _lastWaitTime - elapsed;
                    AnsiConsole.WriteLine($"Waiting for users to read last post: {waiting}ms/{_lastWaitTime}ms");
                    await Task.Delay((int)waiting);
                }
            }
            _timer.Stop();
            _timer.Reset();

            History.Add(processed);

            if (History.Count > 50)
            {
                History.RemoveAt(0);
            }


            if (_urls.Count != 0)
            {
                foreach (var config in _urls)
                {
                    if (config.ShouldProcess)
                    {
                        try
                        {
                            var jsonContent = JsonContent.Create(processed);
                            jsonContent.Headers.Add("ClientSecret", config.Secret);
                            await _httpClient.PostAsync(config.FeedUrl, jsonContent);
                            AnsiConsole.WriteLine($"Forwarded JsonContent to {config.FeedUrl} succesfully.");
                            config.FailedTimes = 0;
                        }
                        catch (Exception)
                        {
                            config.FailedTimes++;
                            AnsiConsole.MarkupLineInterpolated($"[red]***There was an issue sending to {config.FeedUrl} webhook. Failed {config.FailedTimes} times.***[/]");
                        }
                    }
                }
            }

            if (_readingMode)
            {
                _timer.Start();
                // add timer to  class and wait for time remaining of this so processing happens in parallel
                _lastWaitTime = processed.WordCount / 5 * 1000;
            }
        }
    }
}
