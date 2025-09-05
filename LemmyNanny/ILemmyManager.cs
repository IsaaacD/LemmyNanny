using dotNETLemmy.API.Types.Responses;

namespace LemmyNanny
{
    public interface ILemmyManager
    {
        Task<GetPostsResponse> GetNextPosts(CancellationToken token);
        void ResetLastPage();
        Task TryPostReport(int id, string reportReason, CancellationToken token);
    }
}