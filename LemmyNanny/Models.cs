using Microsoft.Extensions.FileSystemGlobbing.Internal;
using System.Text.RegularExpressions;

namespace LemmyNanny
{
    public class PromptContent
    {
        public int Id { get; set; }
        public string? Content { get; set; }
        public List<byte[]> ImageBytes { get; set; } = new List<byte[]>();

        public MatchCollection ImageMatches => Regex.Matches(Content!, @"!\[[^\]]*\]\(([^\s]+[.](png|svg|jpeg|webp))\)",
                                               RegexOptions.None,
                                               TimeSpan.FromSeconds(1));
        public PromptResponse? PromptResponse { get; set; }
    }

    public class PromptResponse
    {
        public bool ReportThis => Result?.StartsWith("Yes") ?? false;
        public string? Result { get; set; }
        public bool Failed { get; set; }
    }

    public enum ProcessedType
    {
        NotSet,
        Comment,
        Post
    }

    public class Processed
    {
        public int PostId { get; set; }
        public int Id { get; set; }
        public string? Url { get; set; }
        public string? Reason { get; set; }
        public string? Content { get; set; }
        public string? Title { get; set; }
        public bool IsReported => Reason?.StartsWith("Yes") ?? false;
        public List<string> History { get; set; } = [];
        public ProcessedType ProcessedType { get; set; }
        public string? Username { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime ProcessedOn { get; set; }
        public string? CreatedDate { get; set; }
        public string? PostUrl { get; set; }
        public string? ExtraInfo { get; set; }
    }


}
