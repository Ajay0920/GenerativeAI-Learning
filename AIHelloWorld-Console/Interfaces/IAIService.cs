using System;

namespace AIHelloWorldConsole.Interfaces
{
    public interface IAIService
    {
        Task<string> GenerateAsync(string propt);
    }
}
