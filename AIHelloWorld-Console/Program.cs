using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using AIHelloWorldConsole.Interfaces;
using AIHelloWorldConsole.Services;

// Configure DI
var services = new ServiceCollection();
services.AddScoped<IAIService, GeminiService>();
using var provider = services.BuildServiceProvider();

var ai = provider.GetRequiredService<IAIService>();
var result = await ai.GenerateAsync("Hello from Program");
Console.WriteLine(result);
