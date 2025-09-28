namespace LemmyNanny
{
    public class Post
    {
        public Timestamp timestamp { get; set; }
        public string operation { get; set; }
        public string schema { get; set; }
        public string table { get; set; }
        public PostData data { get; set; }
        public PostData? previous { get; set; }
    }

    public class PostData
    {
        public int id { get; set; }
        public string url { get; set; }
        public object body { get; set; }
        public string name { get; set; }
        public bool nsfw { get; set; }
        public bool local { get; set; }
        public string apId { get; set; }
        public int creatorId { get; set; }
        public int languageId { get; set; }
        public int communityId { get; set; }
        public bool locked { get; set; }
        public bool deleted { get; set; }
        public bool removed { get; set; }
    }


    public class Comment
    {
        public Timestamp timestamp { get; set; }
        public string operation { get; set; }
        public string schema { get; set; }
        public string table { get; set; }
        public CommentData data { get; set; }
        public CommentData? previous { get; set; }
    }

    public class Timestamp
    {
        public string date { get; set; }
        public int timezone_type { get; set; }
        public string timezone { get; set; }
    }

    public class CommentData
    {
        public int id { get; set; }
        public int creatorId { get; set; }
        public int postId { get; set; }
        public string content { get; set; }
        public bool removed { get; set; }
        public bool deleted { get; set; }
        public string apId { get; set; }
        public bool local { get; set; }
        public bool distinguished { get; set; }
        public string path { get; set; }
    }
}
