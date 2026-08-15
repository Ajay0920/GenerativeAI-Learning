using PRReviewBot.Application.Interfaces;
using PRReviewBot.Application.Models.Common;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PRReviewBot.Infrastructure.GitHub
{
    public sealed class GitHubService : IGitHubService
    {
        private readonly HttpClient _httpClient;

        public GitHubService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<PullRequestData> GetPullRequestAsync(
        string pullRequestUrl,
        CancellationToken cancellationToken = default)
        {
            // ---------------------------------------------
            // 1. Parse GitHub PR URL
            // ---------------------------------------------

            var pullRequestInfo =
            ParsePullRequestUrl(pullRequestUrl);

            // ---------------------------------------------
            // 2. Get PR metadata
            // ---------------------------------------------

            using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"repos/{pullRequestInfo.Owner}/" +
            $"{pullRequestInfo.Repository}/pulls/" +
            $"{pullRequestInfo.Number}");

            request.Headers.Accept.Clear();

            request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
            "application/vnd.github+json"));

            var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

            response.EnsureSuccessStatusCode();

            var json =
            await response.Content.ReadAsStringAsync(
            cancellationToken);

            var pullRequest =
            JsonSerializer.Deserialize<GitHubPullRequestResponse>(
            json);

            if (pullRequest is null)
            {
                throw new InvalidOperationException(
                "Unable to deserialize GitHub pull request response.");
            }

            // ---------------------------------------------
            // 3. Get PR diff
            // ---------------------------------------------

            var diff = await GetPullRequestDiffAsync(
            pullRequestInfo.Owner,
            pullRequestInfo.Repository,
            pullRequestInfo.Number,
            cancellationToken);

            // ---------------------------------------------
            // 4. Map GitHub response to Application model
            // ---------------------------------------------

            return new PullRequestData
            {
                Owner = pullRequestInfo.Owner,
                Repository = pullRequestInfo.Repository,
                PullRequestNumber = pullRequestInfo.Number,

                Title = pullRequest.Title ?? string.Empty,

                Description = pullRequest.Body ?? string.Empty,

                Diff = diff,

                HeadSha = pullRequest.Head?.Sha ?? string.Empty
            };
        }

        // =================================================
        // Get Pull Request Diff
        // =================================================

        private async Task<string> GetPullRequestDiffAsync(
        string owner,
        string repository,
        int pullRequestNumber,
        CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"repos/{owner}/{repository}/pulls/{pullRequestNumber}");

            request.Headers.Accept.Clear();

            request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
            "application/vnd.github.diff"));

            var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync(
            cancellationToken);
        }

        // =================================================
        // Create GitHub Review Comments
        // =================================================

        public async Task AddReviewCommentsAsync(
        PullRequestData pullRequest,
        IEnumerable<ReviewFinding> findings,
        CancellationToken cancellationToken = default)
        {
            var validFindings = findings
            .Where(IsValidFinding)
            .ToList();

            if (validFindings.Count == 0)
            {
                return;
            }

            var comments = validFindings
            .Select(finding => new GitHubReviewComment
            {
                Path = finding.FilePath,

                Line = finding.Line,

                Side = NormalizeSide(finding.Side),

                Body = BuildReviewComment(finding)
            })
            .ToList();

            var reviewRequest = new GitHubReviewRequest
            {
                CommitId = pullRequest.HeadSha,

                Body =
            "## 🤖 AI Code Review\n\n" +
            $"AI identified {comments.Count} " +
            "review finding(s) in this pull request.",

                Event = "COMMENT",

                Comments = comments
            };

            using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"repos/{pullRequest.Owner}/" +
            $"{pullRequest.Repository}/pulls/" +
            $"{pullRequest.PullRequestNumber}/reviews");

            request.Headers.Accept.Clear();

            request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
            "application/vnd.github+json"));

            request.Content =
            JsonContent.Create(reviewRequest);

            var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error =
                await response.Content.ReadAsStringAsync(
                cancellationToken);

                throw new HttpRequestException(
                $"GitHub review creation failed. " +
                $"Status: {(int)response.StatusCode} " +
                $"{response.ReasonPhrase}. " +
                $"Response: {error}");
            }
        }

        // =================================================
        // Build GitHub Comment
        // =================================================

        private static string BuildReviewComment(
        ReviewFinding finding)
        {
            var severity =
            string.IsNullOrWhiteSpace(finding.Severity)
            ? "Review"
            : finding.Severity;

            var className =
            string.IsNullOrWhiteSpace(finding.ClassName)
            ? "Not identified"
            : finding.ClassName;

            var recommendation =
            string.IsNullOrWhiteSpace(
            finding.Recommendation)
            ? "No recommendation provided."
            : finding.Recommendation;

            var suggestedFix =
            string.IsNullOrWhiteSpace(
            finding.SuggestedFix)
            ? "No specific fix provided."
            : finding.SuggestedFix;

            return $"""
### 🤖 AI Code Review — {severity}

**Class:** `{className}`

**Issue:**
{finding.Issue}

**Recommendation:**
{recommendation}

**Possible Fix:**
{suggestedFix}

---
*Generated by PRReviewBot*
""";
        }

        // =================================================
        // Validate Finding
        // =================================================

        private static bool IsValidFinding(
        ReviewFinding finding)
        {
            if (string.IsNullOrWhiteSpace(finding.FilePath))
            {
                return false;
            }

            if (finding.Line <= 0)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(finding.Issue))
            {
                return false;
            }

            return true;
        }

        // =================================================
        // Normalize GitHub Side
        // =================================================

        private static string NormalizeSide(
        string? side)
        {
            return side?.ToUpperInvariant() switch
            {
                "LEFT" => "LEFT",

                _ => "RIGHT"
            };
        }

        // =================================================
        // Parse GitHub PR URL
        // =================================================

        private static PullRequestInfo ParsePullRequestUrl(
        string pullRequestUrl)
        {
            if (!Uri.TryCreate(
            pullRequestUrl,
            UriKind.Absolute,
            out var uri))
            {
                throw new ArgumentException(
                "Invalid GitHub pull request URL.",
                nameof(pullRequestUrl));
            }

            if (!uri.Host.Equals(
            "github.com",
            StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                "URL must be a GitHub URL.",
                nameof(pullRequestUrl));
            }

            var segments = uri.AbsolutePath
            .Trim('/')
            .Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries);

            /*
            * Expected:
            *
            * https://github.com/{owner}/{repo}/pull/{number}
            */

            if (segments.Length != 4 ||
            !segments[2].Equals(
            "pull",
            StringComparison.OrdinalIgnoreCase) ||
            !int.TryParse(
            segments[3],
            out var pullRequestNumber))
            {
                throw new ArgumentException(
                "Invalid GitHub pull request URL. " +
                "Expected format: " +
                "https://github.com/{owner}/{repository}/pull/{number}",
                nameof(pullRequestUrl));
            }

            return new PullRequestInfo(
            segments[0],
            segments[1],
            pullRequestNumber);
        }

        // =================================================
        // GitHub Response Models
        // =================================================

        private sealed class GitHubPullRequestResponse
        {
            public string? Title { get; set; }

            public string? Body { get; set; }

            public GitHubHeadResponse? Head { get; set; }
        }

        private sealed class GitHubHeadResponse
        {
            public string? Sha { get; set; }
        }

        // =================================================
        // GitHub Review Request
        // =================================================

        private sealed class GitHubReviewRequest
        {
            [JsonPropertyName("commit_id")]
            public string CommitId { get; set; } = string.Empty;
            [JsonPropertyName("body")]
            public string Body { get; set; } = string.Empty;
            [JsonPropertyName("event")]
            public string Event { get; set; } = "COMMENT";
            [JsonPropertyName("comments")]
            public List<GitHubReviewComment> Comments { get; set; } = new List<GitHubReviewComment>();
        }

        // =================================================
        // GitHub Review Comment
        // =================================================

        private sealed class GitHubReviewComment
        {
            [JsonPropertyName("path")]
            public string Path { get; set; } = string.Empty;
            [JsonPropertyName("line")]
            public int Line { get; set; }
            [JsonPropertyName("side")]
            public string Side { get; set; } = "RIGHT";
            [JsonPropertyName("body")]
            public string Body { get; set; } = string.Empty;
        }

        private sealed record PullRequestInfo(
        string Owner,
        string Repository,
        int Number);
    }
}