using LemmyNanny.Interfaces;
using Microsoft.Extensions.Hosting;
using SixLabors.ImageSharp;
using Spectre.Console;

namespace LemmyNanny
{
    public class LemmyNannyWorker : BackgroundService
    {
        private readonly IHistoryManager _historyManager;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IOllamaManager _ollamaManager;
        private readonly ILemmyManager _lemmyManager;
  
        public LemmyNannyWorker(IHistoryManager historyManager, IHttpClientFactory httpClientFactory, IOllamaManager ollamaManager, ILemmyManager lemmyManager)
        {
            _historyManager = historyManager;
            _clientFactory = httpClientFactory;
            _ollamaManager = ollamaManager;
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
                        var postInfo = $"PostId:{post.Post.Id}\r\nTitle: {post.Post.Name}\r\nBody: {post.Post.Body}";

                        AnsiConsole.WriteLine(postInfo);
                        AnsiConsole.WriteLine(post.Post.ApId);

                        var promptContent = new PromptContent
                        {
                            Id = post.Post.Id,
                            Content = postInfo
                        };

                        if (!hasRecord)
                        {
                            try
                            {
                                promptContent.ImageBytes = post.Post?.Url?.Contains("/pictrs/") ?? false ? new[] { await picsHttpClient.GetByteArrayAsync(post.Post.Url) } : null;

                                foreach (var consoleImage in promptContent?.ImageBytes?.Select(bytes => new CanvasImage(bytes)))
                                {
                                    consoleImage.MaxWidth = 40;
                                    AnsiConsole.Write(consoleImage);
                                }

                            }
                            catch (UnknownImageFormatException)
                            {
                                AnsiConsole.WriteLine("");
                                AnsiConsole.MarkupInterpolated($"[red]*** post.Post.Url={post?.Post?.Url}. Cannot process UnknownImageFormatException. Likely type failure. ***[/]");
                                AnsiConsole.WriteLine("");
                            }
                            catch (Exception e)
                            {
                                AnsiConsole.WriteLine($"{DateTime.Now}: Failed {e.GetType()} - {e.Message}");
                            }

                            var content = await _ollamaManager.CheckContent( promptContent );
                            newSeen.Reason = content.Result;
                            newSeen.Url = post.Post.ApId;
                            newSeen.IsYes = promptContent.ReportThis;

                            AnsiConsole.WriteLine("");

                            if (!promptContent.ReportThis)
                            {
                                AnsiConsole.WriteLine($"{DateTime.Now}: Found 'No', did not report {post?.Post?.Id}.");
                            }
                            else
                            {
                                AnsiConsole.MarkupInterpolated($"{DateTime.Now}: [yellow]Found 'Yes', time to report {post?.Post?.Id ?? 0} with resultOutput={content.Result}[/]");
                                await _lemmyManager.TryPostReport(promptContent, cancellationToken);
                            }

                            AnsiConsole.WriteLine($"{DateTime.Now}: Checking comments from {post.Post.Id}");
                            var comments = await _lemmyManager.GetCommentsFromPost(post.Post.Id);

                            foreach (var commentView in comments.Comments)
                            {
                                AnsiConsole.WriteLine($"{DateTime.Now} Checking comment: {commentView.Comment.Content}");
                                var results = await _ollamaManager.CheckContent(new PromptContent { Id= commentView.Comment.Id, Content = commentView.Comment.Content });

                                if (results.ReportThis)
                                {
                                    AnsiConsole.WriteLine("Reported");
                                }
                            }

                            _historyManager.AddRecord(newSeen);
                        }
                        else
                        {
                            AnsiConsole.WriteLine("Seen post, skipping: Waiting 1 second");
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
