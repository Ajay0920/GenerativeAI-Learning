using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PRReviewBot.Application.Services;
using PRReviewBot.Application.Interfaces;

namespace PRReviewBot.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class PRReviewController : ControllerBase
    {
        private readonly IPRReviewService _prReviewService;
        public PRReviewController(IPRReviewService pRReviewService)
        {
            _prReviewService = pRReviewService;
        }

        public class ReviewRequest
        {
            public string PullRequestUrl { get; set; }
        }

        [HttpPost("review")]
        public async Task<IActionResult> Review([FromBody] ReviewRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request?.PullRequestUrl))
            {
                return BadRequest(new { Error = "PullRequestUrl is required." });
            }
            try
            {
                var reviewResult = await _prReviewService.ReviewPullRequestAsync(request.PullRequestUrl, cancellationToken);

                if (!reviewResult.IsSuccess)
                {
                    return StatusCode(StatusCodes.Status502BadGateway, reviewResult);
                }
                return Ok(reviewResult);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred while reviewing the pull request: {ex.Message}");
            }
        }
    }
}
