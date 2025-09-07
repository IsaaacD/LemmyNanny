namespace LemmyNanny.Interfaces
{
    public interface IPictrsManager
    {
        Task<IEnumerable<byte[]>?> GetImageBytes(string url, CancellationToken token);
    }
}