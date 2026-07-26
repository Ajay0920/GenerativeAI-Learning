using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AIHelloWorldConsole.Interfaces;
using AIHelloWorldConsole.Services;
using AIHelloWorldConsole.Configuration;

// Use Generic Host
var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection(GeminiOptions.SectionName));

builder.Services.AddHttpClient();
builder.Services.AddTransient<IAIService, GeminiService>();
var app = builder.Build();
var aiService = app.Services.GetRequiredService<IAIService>();
Console.Write("Ask AI:");
var prompt = Console.ReadLine();

if(!string.IsNullOrWhiteSpace(prompt))
{
    var response = await aiService.GenerateAsync(prompt);
    Console.WriteLine();
    Console.WriteLine($"AI Response:");
    Console.WriteLine("----------------");
    Console.WriteLine(response);

}
