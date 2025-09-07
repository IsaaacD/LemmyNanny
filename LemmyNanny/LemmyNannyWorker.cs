using LemmyNanny.Interfaces;
using Microsoft.Extensions.Hosting;
using Spectre.Console;

namespace LemmyNanny
{
    public class LemmyNannyWorker : BackgroundService
    {
        private readonly IHistoryManager _historyManager;
        private readonly IPictrsManager _pictrsManager;
        private readonly IOllamaManager _ollamaManager;
        private readonly ILemmyManager _lemmyManager;
  
        public LemmyNannyWorker(IHistoryManager historyManager, IPictrsManager pictrsManager, IOllamaManager ollamaManager, ILemmyManager lemmyManager)
        {
            _historyManager = historyManager;
            _pictrsManager = pictrsManager;
            _ollamaManager = ollamaManager;
            _lemmyManager = lemmyManager;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {

            _historyManager.SetupDatabase();

            while (!cancellationToken.IsCancellationRequested)
            {
                var postResponse = await _lemmyManager.GetNextPosts(cancellationToken);

                AnsiConsole.WriteLine($"{DateTime.Now}: Checking {postResponse.Posts?.Length ?? 0} posts");
                if(postResponse.Posts == null)
                {
                    _lemmyManager.ResetLastPostPage();
                }
                else
                {
                    foreach (var postView in postResponse.Posts)
                    {
                        var post = postView;
                        var hasRecord = _historyManager.HasPostRecord(post.Post.Id, out _);
              

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
                            promptContent.ImageBytes = await _pictrsManager.GetImageBytes(post.Post.Url ?? "", cancellationToken);
                            
                            var content = await _ollamaManager.CheckContent( promptContent );

                            AnsiConsole.WriteLine("");

                            if (!promptContent.ReportThis)
                            {
                                AnsiConsole.WriteLine($"{DateTime.Now}: Found 'No', did not report {post?.Post?.Id}.");
                            }
                            else
                            {
                                AnsiConsole.MarkupInterpolated($"{DateTime.Now}: [yellow]Found 'Yes', time to report {post?.Post?.Id!} with resultOutput={content.Result}[/]");
                                await _lemmyManager.TryPostReport(promptContent, cancellationToken);
                            }

                            AnsiConsole.WriteLine($"{DateTime.Now}: Checking comments from {post!.Post.Id}");
                            var comments = await _lemmyManager.GetCommentsFromPost(post.Post.Id);
                            AnsiConsole.WriteLine($"{DateTime.Now}: found {comments.Comments.Length} comments");
                            foreach (var commentView in comments.Comments)
                            {
                                var hasSeen = _historyManager.HasCommentRecord(commentView.Comment.Id, out _);
                                if (!hasSeen)
                                {
                                    AnsiConsole.WriteLine($"{DateTime.Now}: Checking comment: {commentView.Comment.Content}");
                                    var commentContent = new PromptContent { Id = commentView.Comment.Id, Content = commentView.Comment.Content };
                                    //commentContent.ImageBytes = await _pictrsManager.GetImageBytes(commentView.Comment.ApId)
                                    var results = await _ollamaManager.CheckContent(commentContent);

                                    if (results.ReportThis)
                                    {
                                        await _lemmyManager.TryCommentReport(commentContent, cancellationToken);
                                    }

                                    _historyManager.AddCommentRecord(new ProcessedComment { CommentId = commentView.Comment.Id, PostId = commentView.Comment.PostId, Url = commentView.Comment.ApId, Reason = results.Result! });
                                }
                                else
                                {
                                    AnsiConsole.WriteLine($"{DateTime.Now}: Already seen comment {commentView.Comment.Id}, skipped.");
                                }
                            }

                            _historyManager.AddPostRecord(new ProcessedPost
                            {
                                PostId = post.Post.Id,
                                Reason = content.Result,
                                Url = post.Post.ApId
                            });
                        }
                        else
                        {
                            AnsiConsole.WriteLine($"{DateTime.Now}: Seen post {post.Post.Id}, skipping: Waiting 1 second");
                            await Task.Delay(1000);
                        }
                    }
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine($"{DateTime.Now}: Cancelled Press enter to end.");
                Console.ReadLine();
            }
        }
    }
}
