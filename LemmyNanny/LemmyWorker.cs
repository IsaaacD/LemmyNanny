using dotNETLemmy.API.Types;
using dotNETLemmy.API.Types.Enums;
using dotNETLemmy.API.Types.Forms;
using Microsoft.Extensions.Hosting;
using OllamaSharp;
using SixLabors.ImageSharp;
using Spectre.Console;
using System.Text;

namespace LemmyNanny
{
    public class LemmyWorker : BackgroundService
    {
        public static string HintTextColor { get; } = "gray";

        public static string AccentTextColor { get; } = "blue";

        public static string WarningTextColor { get; } = "yellow";

        public static string ErrorTextColor { get; } = "red";

        public static string AiTextColor { get; } = "cyan";
        public static string AiThinkTextColor { get; } = "gray";


        private readonly ILemmyHttpClient _lemmyHttpClient;
        private readonly HistoryManager _historyManager;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IOllamaApiClient _ollamaApiClient;
        private string? _lastPage = string.Empty;
        private readonly string _prompt;
        private readonly SortType _sortType;
        private readonly ListingType _listingType;
        public LemmyWorker(ILemmyHttpClient lemmyHttpClient, HistoryManager manager, IHttpClientFactory httpClientFactory, IOllamaApiClient ollamaApiClient, string prompt, SortType sortType, ListingType listingType)
        {
            _lemmyHttpClient = lemmyHttpClient;
            _historyManager = manager;
            _clientFactory = httpClientFactory;
            _ollamaApiClient = ollamaApiClient;
            _prompt = prompt;
            _sortType = sortType;
            _listingType = listingType;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var picsHttpClient = _clientFactory.CreateClient("PictrsClient");

            while (!cancellationToken.IsCancellationRequested)
            {
                var form = new GetPostsForm() {  Sort= _sortType, Type= _listingType };
                if (!string.IsNullOrEmpty(_lastPage)) { 
                    form.PageCursor = _lastPage;
                    AnsiConsole.WriteLine($"set form.PageCursor={_lastPage}");
                }
                var getPostsResponse = await _lemmyHttpClient.GetPosts(form, cancellationToken);
                _lastPage = getPostsResponse.NextPage;
                AnsiConsole.WriteLine($"Checking {getPostsResponse.Posts?.Length ?? 0} posts");
                foreach (var postView in getPostsResponse.Posts)
                {
                    var post = postView;
                    var hasRecord = _historyManager.HasRecord(post.Post.Id, out _);
                    var newSeen = new ProcessedPost { PostId = post.Post.Id };

                    AnsiConsole.WriteLine("");
                    // var postPrompt = $@"You are a moderator on a social media forum, the following is a post that needs to be vetted for community guideline violations. Please output only 'Yes' or 'No' as the answer. If the answer is 'Yes', expand on what the guideline violation could be.";
                    var postPrompt = _prompt + "\r\nPlease output only 'Yes' if violation occurred or 'No' if the content is safe. After 'Yes' or 'No', expand on what the post is about and violations that occurred.";
                    var chat = new Chat(_ollamaApiClient, postPrompt) { Think = false };
                    IAsyncEnumerable<string>? chatResults = null;
                    var postInfo = $"PostId:{post.Post.Id}\r\nTitle: {post.Post.Name}\r\nBody: {post.Post.Body}";
                    AnsiConsole.WriteLine(postInfo);
                    AnsiConsole.WriteLine(post.Post.ApId);
                    if (!hasRecord)
                    {
                        try
                        {
                            if (post.Post?.Url?.Contains("/pictrs/") ?? false)
                            {
                                var imageBytes = new[] { await picsHttpClient.GetByteArrayAsync(post.Post.Url) };
                                AnsiConsole.WriteLine("The following image is compressed for console view, full image goes to the model.");
                                foreach (var consoleImage in imageBytes.Select(bytes => new CanvasImage(bytes)))
                                {
                                    consoleImage.MaxWidth = 40;
                                    AnsiConsole.Write(consoleImage);
                                }
                                chatResults = chat.SendAsync(postInfo, imageBytes, cancellationToken);
                            }
                            else
                            {
                                chatResults = chat.SendAsync(postInfo, cancellationToken);
                            }
                        }
                        catch (UnknownImageFormatException)
                        {
                            AnsiConsole.WriteLine("");
                            AnsiConsole.MarkupInterpolated($"[{ErrorTextColor}]*** post.Post.Url={post?.Post?.Url}. Cannot process UnknownImageFormatException. Likely type failure. ***[/]");
                            AnsiConsole.WriteLine("");
                            chatResults = chat.SendAsync(postInfo, cancellationToken);
                        }
                        catch (Exception e)
                        {
                            AnsiConsole.WriteLine($"Failed {e.GetType()} - {e.Message}");                           
                        }

                        var resultOutput = new StringBuilder();
                        if (chatResults == null)
                        {
                            AnsiConsole.WriteLine($"[{ErrorTextColor}]***chatResults is null. Skipping***[/]");
                            continue;
                        }
                        await foreach (var answerToken in chatResults)
                        {
                            resultOutput.Append(answerToken);
                            AnsiConsole.MarkupInterpolated($"[{AiTextColor}]{answerToken}[/]");
                        }
                        var isNo = resultOutput.ToString().StartsWith("No");
                        newSeen.Reason = resultOutput.ToString();
                        newSeen.Url = post.Post.ApId;
                        AnsiConsole.WriteLine("");
                        if (isNo)
                        {
                            AnsiConsole.WriteLine($"Found 'No', did not report {post?.Post?.Id}.");
                        }
                        else
                        {
                            AnsiConsole.WriteLine($"[{AccentTextColor}]Found 'Yes', time to report {post?.Post?.Id ?? 0} with resultOutput={resultOutput}[/]");

                            if (!string.IsNullOrEmpty(_lemmyHttpClient.Username))
                            {
                                var loginForm = new LoginForm
                                {
                                    UsernameOrEmail = _lemmyHttpClient.Username!,
                                    Password = _lemmyHttpClient.Password!
                                };

                                var loginResponse = await _lemmyHttpClient.Login(loginForm);

                                var report = new CreatePostReportForm() { Auth = loginResponse.Jwt, PostId = post.Post.Id, Reason = resultOutput.ToString() };
                                var resp = await _lemmyHttpClient.CreatePostReport(report);
                                AnsiConsole.WriteLine($"Reported {post?.Post?.Id ?? 0}.");
                            }
                            else
                            {
                                AnsiConsole.WriteLine($"No username, skipped reporting {post?.Post?.Id ?? 0}.");
                            }

                        }

                        _historyManager.AddRecord(newSeen);

                    }
                    else
                    {
                        AnsiConsole.WriteLine("Waiting 1 second");
                        await Task.Delay(1000);
                    }
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Cancelled Press enter to end.");
                Console.ReadLine();
            }
        }
    }
}
