using dotNETLemmy.API.Types;
using dotNETLemmy.API.Types.Enums;
using dotNETLemmy.API.Types.Forms;
using dotNETLemmy.API.Types.Responses;
using LemmyNanny.Interfaces;
using Microsoft.Extensions.Hosting;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LemmyNanny
{
    public class LemmyManager : ILemmyManager
    {
        private string? _lastPostPage = string.Empty;
        private string? _lastCommentPage = string.Empty;
        private readonly SortType _sortType;
        private readonly ListingType _listingType;

        private readonly ILemmyHttpClient _lemmyHttpClient;
        public LemmyManager(ILemmyHttpClient lemmyHttpClient, SortType sortType, ListingType listingType)
        {
            _lemmyHttpClient = lemmyHttpClient;
            _sortType = sortType;
            _listingType = listingType;
        }
        public void ResetLastPage()
        {
            _lastPostPage = string.Empty;
        }
        public async Task<GetPostsResponse> GetNextPosts(CancellationToken token)
        {
            var form = new GetPostsForm() { Sort = _sortType, Type = _listingType };
            if (!string.IsNullOrEmpty(_lastPostPage))
            {
                form.PageCursor = _lastPostPage;
                AnsiConsole.WriteLine($"{DateTime.Now}: set form.PageCursor={_lastPostPage}");
            }
            var getPostsResponse = await _lemmyHttpClient.GetPosts(form, token);
            _lastPostPage = getPostsResponse.NextPage;
            return getPostsResponse;
        }

        public async Task<GetCommentsResponse> GetCommentsFromPost(int id)
        {
            var getCommentsForm = new GetCommentsForm() { PostId = id };
            var comments = await _lemmyHttpClient.GetComments(getCommentsForm);
            return comments;
        }

        public async Task TryPostReport(PromptContent content, CancellationToken token)
        {
            if (!string.IsNullOrEmpty(_lemmyHttpClient.Username))
            {
                var loginForm = new LoginForm
                {
                    UsernameOrEmail = _lemmyHttpClient.Username!,
                    Password = _lemmyHttpClient.Password!
                };

                var loginResponse = await _lemmyHttpClient.Login(loginForm);

                var report = new CreatePostReportForm() { Auth = loginResponse.Jwt, PostId = content.Id, Reason = content.Result };
                var resp = await _lemmyHttpClient.CreatePostReport(report);
                AnsiConsole.WriteLine($"{DateTime.Now}: Reported {content.Id}.");
            }
            else
            {
                AnsiConsole.WriteLine($"{DateTime.Now}: No username, skipped reporting {content.Id}.");
            }
        }
    }
}
