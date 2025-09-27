using LemmyNanny.Helpers;
using LemmyNanny.Interfaces;
using Microsoft.Extensions.Hosting;
using Spectre.Console;

namespace LemmyNanny
{
    public class LemmyNannyOperator(IHistoryManager historyManager, IImagesManager imagesManager, IOllamaManager ollamaManager,
        IWebhooksManager webhooks, ILemmyManager lemmyManager, StartUpStats stats) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(1000); // wait for other service to start too
            historyManager.SetupDatabase();
            await webhooks.SendStartupStats(stats);
            while (!stoppingToken.IsCancellationRequested)
            {
                var gotPost = CommentAndPostBucket.PostItems.TryTake(out var post);

                if (gotPost)
                {
                    AnsiConsole.WriteLine("");
                    var postInfo = $"This is the post:```{Environment.NewLine}Title: {post.Post.Name}{Environment.NewLine}Body: {post.Post.Body}```";

                    AnsiConsole.WriteLine(postInfo);
                    AnsiConsole.WriteLine($"{post.Counts.Comments} comments on post.");
                    AnsiConsole.WriteLine(post.Post.ApId);

                    var promptContent = new PromptContent
                    {
                        Id = post.Post.Id,
                        Content = postInfo,
                        ExtraImages = [post.Post.Url, post.Post.ThumbnailUrl]
                    };
                    promptContent = await imagesManager.GetImageBytes(promptContent, stoppingToken);

                    while (ollamaManager.IsBusy)
                    {
                        await Task.Delay(1000);
                    }

                    var promptResponse = await ollamaManager.CheckContent(promptContent, stoppingToken);

                    AnsiConsole.WriteLine("");

                    if (!promptResponse.ReportThis)
                    {
                        AnsiConsole.WriteLine($"{DateTime.Now}: Found 'No', did not report {post?.Post?.Id}.");
                    }
                    else
                    {
                        AnsiConsole.WriteLine("");
                        AnsiConsole.MarkupInterpolated($"{DateTime.Now}: [yellow]Found 'Yes', time to report {post?.Post?.Id!} with resultOutput={promptResponse.Result}[/]");
                        AnsiConsole.WriteLine("");
                        await lemmyManager.TryPostReport(promptContent, stoppingToken);
                    }

                    var processedPost = new Processed
                    {
                        Id = post!.Post.Id,
                        Reason = promptResponse.Result,
                        Url = post.Post.ApId,
                        Title = post.Post.Name,
                        Content = post.Post.Body,
                        ProcessedType = ProcessedType.Post,
                        Username = post.Creator.DisplayName ?? post.Creator.Name,
                        AvatarUrl = post.Creator.Avatar,
                        ProcessedOn = DateTime.UtcNow,
                        CreatedDate = post.Post.Published,
                        PostUrl = post.Post.ApId,
                        ThumbnailUrl = post.Post.ThumbnailUrl,
                        CommentNumber = post.Counts.Comments.ToString(),
                        CommunityName = post.Community.Name,
                        Failed = promptResponse.Failed,
                        ViewedImages = promptResponse.ImagesProcessed > 0,
                        ExtraInfo = $"Processed {webhooks.Posts} posts and {webhooks.Comments} comments in {webhooks.ElapsedTime.ToReadableString()}."
                    };

                    historyManager.AddPostRecord(processedPost);
                    await webhooks.SendToWebhooksAndUpdateStats(processedPost);
                }

                var gotComment = CommentAndPostBucket.CommentItems.TryTake(out var commentView);
                if (gotComment)
                {
                    // process comment
                    var commentContent = new PromptContent { Id = commentView.Comment.Id, Content = $"This is the comment: ```{commentView.Comment.Content}```" };
                    commentContent = await imagesManager.GetImageBytes(commentContent, stoppingToken);

                    while (ollamaManager.IsBusy)
                    {
                        await Task.Delay(1000);
                    }

                    var commentResponse = await ollamaManager.CheckContent(commentContent, stoppingToken);

                    if (commentResponse.ReportThis)
                    {
                        await lemmyManager.TryCommentReport(commentContent, stoppingToken);
                    }
                    var processedComment = new Processed
                    {
                        Id = commentView.Comment.Id,
                        Content = commentView.Comment.Content,
                        PostId = commentView.Comment.PostId,
                        Title = commentView.Post.Name,
                        Url = commentView.Comment.ApId,
                        Reason = commentResponse.Result ?? "",
                        ProcessedType = ProcessedType.Comment,
                        Username = commentView.Creator.DisplayName ?? commentView.Creator.Name,
                        AvatarUrl = commentView.Creator.Avatar,
                        ProcessedOn = DateTime.UtcNow,
                        CommunityName = commentView.Community.Name,
                        CreatedDate = commentView.Comment.Published,
                        PostUrl = commentView.Post.ApId,
                        CommentNumber = " ",
                        Failed = commentResponse.Failed,
                        ViewedImages = commentResponse.ImagesProcessed > 0,
                        ExtraInfo = $"Processed {webhooks.Posts} posts and {webhooks.Comments} comments in {webhooks.ElapsedTime.ToReadableString()}."
                    };
                    historyManager.AddCommentRecord(processedComment);
                    await webhooks.SendToWebhooksAndUpdateStats(processedComment);
                }
            }
        }
    }
}
