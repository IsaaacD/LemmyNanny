using Microsoft.Extensions.Hosting;
using OllamaSharp;
using SixLabors.ImageSharp;
using Spectre.Console;
using System.Text;

namespace LemmyNanny
{
    public class LemmyNannyWorker : BackgroundService
    {
        private readonly IHistoryManager _historyManager;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IOllamaApiClient _ollamaApiClient;
        private readonly ILemmyManager _lemmyManager;
        private readonly string _prompt;
  
        public LemmyNannyWorker(IHistoryManager historyManager, IHttpClientFactory httpClientFactory, IOllamaApiClient ollamaApiClient, string prompt, ILemmyManager lemmyManager)
        {
            _historyManager = historyManager;
            _clientFactory = httpClientFactory;
            _ollamaApiClient = ollamaApiClient;
            _prompt = prompt;
            _lemmyManager = lemmyManager;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var picsHttpClient = _clientFactory.CreateClient("PictrsClient");
            _historyManager.SetupDatabase();
            while (!cancellationToken.IsCancellationRequested)
            {
                var postResponse = await _lemmyManager.GetNextPosts(cancellationToken);

                AnsiConsole.WriteLine($"{DateTime.Now}: Checking {postResponse.Posts?.Length ?? 0} posts");
                if(postResponse.Posts == null)
                {
                    _lemmyManager.ResetLastPage();
                }
                else
                {
                    foreach (var postView in postResponse.Posts)
                    {
                        var post = postView;
                        var hasRecord = _historyManager.HasRecord(post.Post.Id, out _);
                        var newSeen = new ProcessedPost { PostId = post.Post.Id };

                        AnsiConsole.WriteLine("");
                        var postPrompt = _prompt + "\r\nPlease output only 'Yes' if violation occurred or 'No' if the content is safe. After 'Yes' or 'No', expand on what the post is about and violations that occurred.";
                        var chat = new Chat(_ollamaApiClient, postPrompt) { Think = false };
                        IAsyncEnumerable<string>? chatResults = null;
                        var postInfo = $"PostId:{post.Post.Id}\r\nTitle: {post.Post.Name}\r\nBody: {post.Post.Body}";
                        AnsiConsole.WriteLine(postInfo);
                        AnsiConsole.WriteLine(post.Post.ApId);
                        if (!hasRecord)
                        {
                            try
                            {
                                if (post.Post?.Url?.Contains("/pictrs/") ?? false)
                                {
                                    var imageBytes = new[] { await picsHttpClient.GetByteArrayAsync(post.Post.Url) };
                                    AnsiConsole.WriteLine("The following image is compressed for console view, full image goes to the model.");
                                    foreach (var consoleImage in imageBytes.Select(bytes => new CanvasImage(bytes)))
                                    {
                                        consoleImage.MaxWidth = 40;
                                        AnsiConsole.Write(consoleImage);
                                    }
                                    chatResults = chat.SendAsync(postInfo, imageBytes, cancellationToken);
                                }
                                else
                                {
                                    chatResults = chat.SendAsync(postInfo, cancellationToken);
                                }
                            }
                            catch (UnknownImageFormatException)
                            {
                                AnsiConsole.WriteLine("");
                                AnsiConsole.MarkupInterpolated($"[red]*** post.Post.Url={post?.Post?.Url}. Cannot process UnknownImageFormatException. Likely type failure. ***[/]");
                                AnsiConsole.WriteLine("");
                                chatResults = chat.SendAsync(postInfo, cancellationToken);
                            }
                            catch (Exception e)
                            {
                                AnsiConsole.WriteLine($"{DateTime.Now}: Failed {e.GetType()} - {e.Message}");
                            }

                            var resultOutput = new StringBuilder();
                            if (chatResults == null)
                            {
                                AnsiConsole.MarkupInterpolated($"{DateTime.Now}: [red]***chatResults is null. Skipping***[/]");
                                continue;
                            }
                            await foreach (var answerToken in chatResults)
                            {
                                resultOutput.Append(answerToken);
                                AnsiConsole.MarkupInterpolated($"[yellow]{answerToken}[/]");
                            }
                            var isYes = resultOutput.ToString().StartsWith("Yes");
                            newSeen.Reason = resultOutput.ToString();
                            newSeen.Url = post.Post.ApId;
                            newSeen.IsYes = isYes;
                            AnsiConsole.WriteLine("");

                            if (!isYes)
                            {
                                AnsiConsole.WriteLine($"{DateTime.Now}: Found 'No', did not report {post?.Post?.Id}.");
                            }
                            else
                            {
                                AnsiConsole.MarkupInterpolated($"{DateTime.Now}: [yellow]Found 'Yes', time to report {post?.Post?.Id ?? 0} with resultOutput={resultOutput}[/]");
                                await _lemmyManager.TryPostReport(post.Post.Id, resultOutput.ToString(), cancellationToken);
                            }

                            _historyManager.AddRecord(newSeen);

                        }
                        else
                        {
                            AnsiConsole.WriteLine("Waiting 1 second");
                            await Task.Delay(1000);
                        }
                    }
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                _historyManager.UpdateEndTime();
                Console.WriteLine("Cancelled Press enter to end.");
                Console.ReadLine();
            }
        }
    }
}
