using System.Threading.Tasks;
using AIHelloWorldConsole.Interfaces;

namespace AIHelloWorldConsole.Services
{
    public class GeminiService : IAIService
    {
        public Task<string> GenerateAsync(string propt)
        {
            // Simple synchronous implementation for now
            return Task.FromResult($"Gemini generated response for: {propt}");
        }
    }
}
