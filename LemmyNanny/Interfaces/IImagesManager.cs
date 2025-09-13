namespace LemmyNanny.Interfaces
{
    public interface IImagesManager
    {
        Task<PromptContent> GetImageBytes(PromptContent content, CancellationToken token);
        Task<byte[]?> GetImageBytes(string? url, CancellationToken token = default);
    }
}