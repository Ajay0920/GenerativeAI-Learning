using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelloWorld.Console.Interfaces
{
    public interface IAIService
    {
        Task<string> GenerateAsync(string prompt);
    }
}
