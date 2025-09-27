using dotNETLemmy.API.Types;
using dotNETLemmy.API.Types.Enums;
using dotNETLemmy.API.Types.Forms;
using dotNETLemmy.API.Types.Responses;
using LemmyNanny.Interfaces;
using Spectre.Console;

namespace LemmyNanny
{
    public class LemmyManager : ILemmyManager
    {
        private string? _lastPostPage = string.Empty;
        private readonly SortType _sortType;
        private readonly ListingType _listingType;

        private readonly ILemmyHttpClient _lemmyHttpClient;
        public LemmyManager(ILemmyHttpClient lemmyHttpClient, SortType sortType, ListingType listingType)
        {
            _lemmyHttpClient = lemmyHttpClient;
            _sortType = sortType;
            _listingType = listingType;
        }
        public void ResetLastPostPage()
        {
            _lastPostPage = string.Empty;
        }

        public async Task<PostView> GetPost(int id, CancellationToken token = default, int retry = 0)
        {
            try
            {
                var form = new GetPostForm() { Id = id };

                var getPostsResponse = await _lemmyHttpClient.GetPost(form, token);

                return getPostsResponse.PostView;
            }
            catch (Exception)
            {
                if (retry < 3)
                {
                    AnsiConsole.WriteLine($"{DateTime.Now}: Failed {nameof(GetPost)}, retrying");
                    await Task.Delay(100);
                    return await GetPost(id, token, ++retry);
                }
                else
                {
                    return await Task.FromResult(new PostView());
                }
            }
        }

        public async Task<CommentView> GetComment(int id, CancellationToken token = default, int retry = 0)
        {
            try
            {
                var form = new GetCommentForm() { Id = id };
                

                var getPostsResponse = await _lemmyHttpClient.GetComment(form, token);

                return getPostsResponse.CommentView;
            }
            catch (Exception)
            {
                if (retry < 3)
                {
                    AnsiConsole.WriteLine($"{DateTime.Now}: Failed {nameof(GetComment)}, retrying");
                    await Task.Delay(100);
                    return await GetComment(id, token, ++retry);
                }
                else
                {
                    return await Task.FromResult(new CommentView());
                }
            }
        }

        public async Task<List<PostView>> GetNextPosts(CancellationToken token = default, int retry = 0)
        {
            try
            {
                var form = new GetPostsForm() { Sort = _sortType, Type = _listingType };
                if (!string.IsNullOrEmpty(_lastPostPage))
                {
                    form.PageCursor = _lastPostPage;
                    AnsiConsole.WriteLine($"{DateTime.Now}: set form.PageCursor={_lastPostPage}");
                }
                var getPostsResponse = await _lemmyHttpClient.GetPosts(form, token);
                _lastPostPage = getPostsResponse.NextPage;
                //var response = getPostsResponse.Posts.Select(o =>
                //new Post
                //{ 
                //    data = new PostData
                //    {
                //        apId = o.Post.ApId,
                //        body = o.Post.Body,
                //        name = o.Post.Name,
                //        url = o.Post.Url,
                //        id = o.Post.Id,
                        
                //    }
                //});
                return getPostsResponse.Posts.ToList();
            }
            catch (Exception)
            {
                if (retry < 3)
                {
                    AnsiConsole.WriteLine($"{DateTime.Now}: Failed {nameof(GetNextPosts)}, retrying");
                    await Task.Delay(100);
                    return await GetNextPosts(token, ++retry);
                }
                else
                {
                    return await Task.FromResult(new List<PostView>());
                }
            }

        }

        public async Task<GetCommentsResponse> GetCommentsFromPost(int id, int page = 1, CancellationToken token = default, int retry = 0)
        {
            try
            {
                var getCommentsForm = new GetCommentsForm() { PostId = id, Page = page };
                var comments = await _lemmyHttpClient.GetComments(getCommentsForm, token);
                return comments;
            }
            catch (Exception)
            {
                if(retry < 3)
                {
                    AnsiConsole.WriteLine($"{DateTime.Now}: Failed {nameof(GetCommentsFromPost)}, retrying");
                    await Task.Delay(100);
                    return await GetCommentsFromPost(id, page, token, ++retry);
                }
                else
                {
                    return await Task.FromResult(new GetCommentsResponse());
                }
            }
        }

        public async Task<bool> TryCommentReport(PromptContent content, CancellationToken token = default)
        {
            if (!string.IsNullOrEmpty(_lemmyHttpClient.Username) && content.PromptResponse != null)
            {
                var loginForm = new LoginForm
                {
                    UsernameOrEmail = _lemmyHttpClient.Username!,
                    Password = _lemmyHttpClient.Password!
                };

                var loginResponse = await _lemmyHttpClient.Login(loginForm);

                var report = new CreateCommentReportForm() { Auth = loginResponse.Jwt, CommentId = content.Id, Reason = content.PromptResponse!.Result! };
                var resp = await _lemmyHttpClient.SendAsync<CommentReportResponse>(report);
                AnsiConsole.WriteLine($"{DateTime.Now}: Reported {content.Id}.");
                return true;

            }
            else
            {
                AnsiConsole.WriteLine($"{DateTime.Now}: No username, skipped reporting {content.Id}.");
                return false;
            }
        }

        public async Task<bool> TryPostReport(PromptContent content, CancellationToken token = default)
        {
            if (!string.IsNullOrEmpty(_lemmyHttpClient.Username) && content.PromptResponse != null)
            {
                var loginForm = new LoginForm
                {
                    UsernameOrEmail = _lemmyHttpClient.Username!,
                    Password = _lemmyHttpClient.Password!
                };

                var loginResponse = await _lemmyHttpClient.Login(loginForm);

                var report = new CreatePostReportForm() { Auth = loginResponse.Jwt, PostId = content.Id, Reason = content.PromptResponse.Result! };
                var resp = await _lemmyHttpClient.CreatePostReport(report);
                AnsiConsole.WriteLine($"{DateTime.Now}: Reported {content.Id}.");
                return true;
            }
            else
            {
                AnsiConsole.WriteLine($"{DateTime.Now}: No username, skipped reporting {content.Id}.");
                return false;
            }
        }

        public async Task Setup()
        {
            await Task.FromResult(Task.CompletedTask);
        }
    }
}
