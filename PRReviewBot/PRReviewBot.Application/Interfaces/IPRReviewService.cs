using PRReviewBot.Application.Models.Common;

namespace PRReviewBot.Application.Interfaces
{
    public interface IPRReviewService
    {
        Task<AIResult> ReviewPullRequestAsync(
          string pullRequestUrl,
          CancellationToken cancellationToken = default);

    }
}
