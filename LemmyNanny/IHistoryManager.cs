namespace LemmyNanny
{
    public interface IHistoryManager
    {
        void AddRecord(ProcessedPost post);
        bool HasRecord(int id, out string timestamp);
        void SetupDatabase();
        void UpdateEndTime();
    }
}