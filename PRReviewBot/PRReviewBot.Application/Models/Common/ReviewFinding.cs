namespace PRReviewBot.Application.Models.Common
{
    public class ReviewFinding
    {
        public string FilePath { get; set; } = string.Empty;
        public int Line { get; set; }
        public string Side { get; set; } = "RIGHT";
        public string ClassName { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Issue { get; set; } = string.Empty;
        public string Recommendation { get; set; }
        public string? SuggestedFix { get; set; }

    }
}
