using PRReviewBot.Application.Models.Common;

namespace PRReviewBot.Application.Interfaces
{
    public interface IAIService
    {
        Task<AIResult> GenerateAsync(string prompt, CancellationToken cancellationToken = default);
    }
}
