using LemmyNanny.Interfaces;
using Microsoft.Extensions.Hosting;
using Spectre.Console;
using System.Text.RegularExpressions;

namespace LemmyNanny
{
    public class LemmyNannyWorker : BackgroundService
    {
        private readonly IHistoryManager _historyManager;
        private readonly IImagesManager _imagesManager;
        private readonly IOllamaManager _ollamaManager;
        private readonly ILemmyManager _lemmyManager;
        private readonly IWebhooksManager _webhooks;
  
        public LemmyNannyWorker(IHistoryManager historyManager, IImagesManager imagesManager, IOllamaManager ollamaManager, ILemmyManager lemmyManager, IWebhooksManager webhooks)
        {
            _historyManager = historyManager;
            _imagesManager = imagesManager;
            _ollamaManager = ollamaManager;
            _lemmyManager = lemmyManager;
            _webhooks = webhooks;
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
                        var postInfo = $"This is the post:```\r\nTitle: {post.Post.Name}\r\nBody: {post.Post.Body}```";

                        AnsiConsole.WriteLine(postInfo);
                        AnsiConsole.WriteLine($"{post.Counts.Comments} comments on post.");
                        AnsiConsole.WriteLine(post.Post.ApId);

                        var promptContent = new PromptContent
                        {
                            Id = post.Post.Id,
                            Content = postInfo
                        };

                        if (!hasRecord)
                        {
                            var urlBytes = await _imagesManager.GetImageBytes(post.Post.Url ?? "", cancellationToken);
                            if(urlBytes != null)
                                promptContent.ImageBytes.Add(urlBytes);

                            promptContent = await _imagesManager.GetImageBytes(promptContent, cancellationToken);

                            var content = await _ollamaManager.CheckContent( promptContent, cancellationToken );

                            AnsiConsole.WriteLine("");

                            if (!content.ReportThis)
                            {
                                AnsiConsole.WriteLine($"{DateTime.Now}: Found 'No', did not report {post?.Post?.Id}.");
                            }
                            else
                            {
                                AnsiConsole.WriteLine("");
                                AnsiConsole.MarkupInterpolated($"{DateTime.Now}: [yellow]Found 'Yes', time to report {post?.Post?.Id!} with resultOutput={content.Result}[/]");
                                AnsiConsole.WriteLine("");
                                await _lemmyManager.TryPostReport(promptContent, cancellationToken);
                            }

                            var processedPost = new Processed
                            {
                                Id = post!.Post.Id,
                                Reason = content.Result,
                                Url = post.Post.ApId,
                                Title = post.Post.Name,
                                Content = post.Post.Body,
                                ProcessedType = ProcessedType.Post
                            };

                            _historyManager.AddPostRecord(processedPost);
                            await _webhooks.SendToWebhooksAndUpdateStats(processedPost);

                            if (post!.Counts.Comments > 0)
                            {
                                AnsiConsole.WriteLine($"Post has {post.Counts.Comments} comments, attempting to process them now.");
                                var currentCount = 0;
                                var currentPage = 1;
                                while (currentCount < post.Counts.Comments)
                                {
                                    AnsiConsole.WriteLine($"{DateTime.Now}: Checking comments from {post!.Post.Id}");
                                    var comments = await _lemmyManager.GetCommentsFromPost(post.Post.Id, currentPage++, cancellationToken);
                                    AnsiConsole.WriteLine($"{DateTime.Now}: found {comments.Comments.Length} comments");

                                    foreach (var commentView in comments.Comments)
                                    {
                                  
                                        AnsiConsole.WriteLine($"Processing {++currentCount}/{post.Counts.Comments} comments.");
                                        var hasSeen = _historyManager.HasCommentRecord(commentView.Comment.Id, out _);
                                        if (!hasSeen)
                                        {
                                            AnsiConsole.WriteLine($"{DateTime.Now}: Checking comment: {commentView.Comment.Content}");
                                            var commentContent = new PromptContent { Id = commentView.Comment.Id, Content = $"This is the comment: ```{commentView.Comment.Content}```" };
                                            commentContent = await _imagesManager.GetImageBytes(commentContent, cancellationToken);

                                            var results = await _ollamaManager.CheckContent(commentContent, cancellationToken);

                                            if (results.ReportThis)
                                            {
                                                await _lemmyManager.TryCommentReport(commentContent, cancellationToken);
                                            }
                                            var processedComment = new Processed { 
                                                Id = commentView.Comment.Id, 
                                                Content=commentView.Comment.Content, 
                                                PostId = commentView.Comment.PostId, 
                                                Url = commentView.Comment.ApId, 
                                                Reason = results.Result ?? "" ,
                                                ProcessedType = ProcessedType.Comment
                                            };
                                            _historyManager.AddCommentRecord(processedComment);
                                            await _webhooks.SendToWebhooksAndUpdateStats(processedComment);
                                        }
                                        else
                                        {
                                            AnsiConsole.WriteLine($"{DateTime.Now}: Already seen comment {commentView.Comment.Id}, skipped.");
                                        }
                                    }
                                }
                            }

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
