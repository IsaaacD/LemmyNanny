using dotNETLemmy.API.Types;
using dotNETLemmy.API.Types.Responses;

namespace LemmyNanny.Interfaces
{
    public interface ILemmyManager
    {
        Task Setup();
        Task<List<PostView>> GetNextPosts(CancellationToken token, int retry = 0);
        void ResetLastPostPage();
        Task<bool> TryPostReport(PromptContent content, CancellationToken token);
        Task<GetCommentsResponse> GetCommentsFromPost(int id, int page=1, CancellationToken token = default, int retry = 0);
        Task<bool> TryCommentReport(PromptContent content, CancellationToken token);
        Task<PostView> GetPost(int id, CancellationToken token = default, int retry = 0);
        Task<CommentView> GetComment(int id, CancellationToken token = default, int retry = 0);
    }
}