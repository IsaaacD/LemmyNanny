namespace LemmyNanny.Interfaces
{
    public interface IHistoryManager
    {
        void AddCommentRecord(Processed comment);
        void AddPostRecord(Processed post);
        bool HasPostRecord(int id, out string timestamp);
        bool HasCommentRecord(int id, out string timestamp);
        void SetupDatabase();
    }
}