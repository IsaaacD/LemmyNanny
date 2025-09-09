using Microsoft.Extensions.FileSystemGlobbing.Internal;
using System.Text.RegularExpressions;

namespace LemmyNanny
{
    public class PromptContent
    {
        public int Id { get; set; }
        public bool ReportThis => Result?.StartsWith("Yes") ?? false;
        public string? Content { get; set; }
        public List<byte[]> ImageBytes { get; set; } = new List<byte[]>();
        public string? Result { get; set; }

        public MatchCollection ImageMatches => Regex.Matches(Content!, @"!\[[^\]]*\]\(([^\s]+[.](png|svg|jpeg|webp))\)",
                                               RegexOptions.None,
                                               TimeSpan.FromSeconds(1));
        public bool Failed { get; set; }
    }

    public class ProcessedComment : Processed
    {
        public int PostId { get; set; }
    }

    public class ProcessedPost : Processed
    {

    }

    public abstract class Processed
    {
        public int Id { get; set; }
        public string? Url { get; set; }
        public string? Reason { get; set; }
        public bool IsYes => Reason?.StartsWith("Yes") ?? false;
        public List<string> History { get; set; } = [];
    }


}
