using dotNETLemmy.API.Types.Responses;

namespace LemmyNanny.Interfaces
{
    public interface ILemmyManager
    {
        Task<GetPostsResponse> GetNextPosts(CancellationToken token);
        void ResetLastPostPage();
        Task<bool> TryPostReport(PromptContent content, CancellationToken token);
        Task<GetCommentsResponse> GetCommentsFromPost(int id, int page=1);
        Task<bool> TryCommentReport(PromptContent content, CancellationToken token);
    }
}