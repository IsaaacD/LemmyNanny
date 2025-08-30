using dotNETLemmy.API.Types;
using dotNETLemmy.API.Types.Forms;
using dotNETLemmy.API.Types.Responses;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using OllamaSharp;
using OllamaSharp.Models.Chat;
using SixLabors.ImageSharp;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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

        public string BaseAddress
        {
            get => _lemmyHttpClient.BaseAddress;
            set => _lemmyHttpClient.BaseAddress = value;
        }

        public string SqliteConnection { get; set; }
        public string LemmyUserName { get; set; }
        public string LemmyPassword { get; set; }

        public string OllamaUrl { get; set; }
        public string OllamaModel { get; set; }

        private HttpClient _picsHttpClient = new HttpClient();

        private readonly ILemmyHttpClient _lemmyHttpClient;
        private readonly HistoryManager _historyManager;

        private string? _lastPage = string.Empty;

        public LemmyWorker(ILemmyHttpClient lemmyHttpClient, HistoryManager manager)
        {
            _lemmyHttpClient = lemmyHttpClient;
            _historyManager = manager;
        }
            


        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var ollamaHttpClient = new HttpClient() { BaseAddress = new Uri(OllamaUrl), Timeout= TimeSpan.FromMinutes(10) };
            var ollama = new OllamaApiClient(ollamaHttpClient);
            ollama.SelectedModel = OllamaModel;
            

            while (!cancellationToken.IsCancellationRequested)
            {
                var form = new GetPostsForm() {  Sort=dotNETLemmy.API.Types.Enums.SortType.Hot, Type= dotNETLemmy.API.Types.Enums.ListingType.All};
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
                    var postPrompt = $@"You are a moderator on a social media forum, the following is a post that needs to be vetted for community guideline violations. Please output only 'Yes' or 'No' as the answer. If the answer is 'Yes', expand on what the guideline violation could be.";
                    var chat = new Chat(ollama, postPrompt) { Think = false };
                    IAsyncEnumerable<string>? chatResults = null;
                    var postInfo = $"PostId:{post.Post.Id}\r\nTitle: {post.Post.Name}\r\nBody: {post.Post.Body}";
                    AnsiConsole.WriteLine(postInfo);

                    if (!hasRecord)
                    {
                        try
                        {
                            if (post.Post?.Url?.Contains("/pictrs/") ?? false)
                            {
                                var imageBytes = new[] { await _picsHttpClient.GetByteArrayAsync(post.Post.Url) };
                                AnsiConsole.WriteLine("The following image is compressed for console view, full image goes to the model.");
                                //AnsiConsole.Write("The following image is compressed for console view, full image goes to the model.");
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
                            AnsiConsole.WriteLine($"Failed {e.GetType()}");

                        }

                        var resultOutput = new StringBuilder();
                        if (chatResults == null)
                        {
                            return;
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
                            AnsiConsole.WriteLine($"Found 'Yes', time to report {post?.Post?.Id ?? 0} with resultOutput={resultOutput}");


                            var loginForm = new LoginForm
                            {
                                UsernameOrEmail = LemmyUserName,
                                Password = LemmyPassword
                            };

                            var loginResponse = await _lemmyHttpClient.Login(loginForm);

                            var report = new CreatePostReportForm() { Auth = loginResponse.Jwt, PostId = post.Post.Id, Reason = resultOutput.ToString() };
                            //var resp = await _lemmyHttpClient.CreatePostReport(report);
                            //AnsiConsole.WriteLine($"Reported {post?.Post?.Id ?? 0}.");
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
