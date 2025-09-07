using dotNETLemmy.API.Types.Responses;

namespace LemmyNanny.Interfaces
{
    public interface ILemmyManager
    {
        Task<GetPostsResponse> GetNextPosts(CancellationToken token);
        void ResetLastPostPage();
        void ResetLastCommentPage();
        Task TryPostReport(PromptContent content, CancellationToken token);
        Task<GetCommentsResponse> GetCommentsFromPost(int id);
        Task TryCommentReport(PromptContent content, CancellationToken token);
    }
}