namespace LemmyNanny.Interfaces
{
    public interface IOllamaManager
    {
        Task<PromptContent> CheckContent(PromptContent content, CancellationToken cancellation = default);
    }
}