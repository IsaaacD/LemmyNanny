using dotNETLemmy.API.Types;
using LemmyNanny.Interfaces;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;

namespace LemmyNanny
{
    /// <summary>
    /// Consumes Lemmy feeds based on configured SortType and ListingTypes
    /// </summary>
    /// <param name="lemmyManager"></param>
    public class HttpRestConsumer(ILemmyManager lemmyManager) : BackgroundService, ILemmyConsumer
    {


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var postResponse = await lemmyManager.GetNextPosts(stoppingToken); // handles pages for getting next coments

                foreach (var item in postResponse)
                {

                    CommentAndPostBucket.PostItems.TryAdd(item);

                    if (item!.Counts.Comments > 0)
                    {
                        var currentCount = 0;
                        var currentPage = 1;
                        while (currentCount < item.Counts.Comments)
                        {
                            var comments = await lemmyManager.GetCommentsFromPost(item.Post.Id, currentPage, stoppingToken); // takes 10 comments, but iterates over pages

                            foreach (var comment in comments.Comments)
                            {
                                CommentAndPostBucket.CommentItems.TryAdd(comment);
                            }
                        }
                    }
                }
            }
        }
    }
}
