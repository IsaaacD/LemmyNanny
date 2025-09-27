using dotNETLemmy.API.Types;
using System.Collections.Concurrent;

namespace LemmyNanny
{
    public static class CommentAndPostBucket
    {
        private const int COMMENT_COUNT = 1000;
        private const int POST_COUNT = 100;
        public static BlockingCollection<CommentView> CommentItems { get; set; } = new(COMMENT_COUNT);
        public static BlockingCollection<PostView> PostItems { get; set; } = new(POST_COUNT);
    }
}
