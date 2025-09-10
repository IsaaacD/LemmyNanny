namespace LemmyNanny.Interfaces
{
    public interface IOllamaManager
    {
        Task<PromptResponse> CheckContent(PromptContent content, CancellationToken cancellation = default);
    }
}