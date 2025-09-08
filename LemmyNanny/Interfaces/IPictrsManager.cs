namespace LemmyNanny.Interfaces
{
    public interface IPictrsManager
    {
        Task<byte[]?> GetImageBytes(string url, CancellationToken token);
    }
}