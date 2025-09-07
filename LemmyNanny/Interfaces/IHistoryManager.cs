namespace LemmyNanny.Interfaces
{
    public interface IHistoryManager
    {
        void AddCommentRecord(ProcessedComment comment);
        void AddPostRecord(ProcessedPost post);
        bool HasPostRecord(int id, out string timestamp);
        bool HasCommentRecord(int id, out string timestamp);
        void SetupDatabase();
    }
}