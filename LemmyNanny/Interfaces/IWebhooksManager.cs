namespace LemmyNanny.Interfaces
{
    public interface IWebhooksManager
    {
        int Comments { get; }
        int CommentsFlagged { get; }
        TimeSpan ElapsedTime { get; }
        List<Processed> History { get; set; }
        int Posts { get; }
        int PostsFlagged { get; }
        DateTime StartTime { get; }
        Task SendToWebhooksAndUpdateStats(Processed processed);
    }
}