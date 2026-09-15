using System.ClientModel;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OpenAI;
using OpenAI.Chat;

using SentinelCase.AI.Services;
using SentinelCase.Application.Common.Interfaces;

namespace SentinelCase.AI;

public static class DependencyInjection
{
    public static IServiceCollection AddAI(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var apiKey =
            configuration["AI:GroqApiKey"]
            ?? Environment.GetEnvironmentVariable("GROQ_API_KEY")
            ?? throw new InvalidOperationException(
                "AI:GroqApiKey (or GROQ_API_KEY) is missing.");

        var modelId =
            configuration["AI:Model"]
            ?? "openai/gpt-oss-120b";

        services.AddSingleton(_ =>
        {
            var options = new OpenAIClientOptions
            {
                Endpoint = new Uri("https://api.groq.com/openai/v1"),
            };

            return new ChatClient(
                modelId,
                new ApiKeyCredential(apiKey),
                options);
        });

        services.AddScoped<IIncidentAiAnalysisService>(serviceProvider =>
        {
            var chatClient = serviceProvider
                .GetRequiredService<ChatClient>();

            var logger = serviceProvider
                .GetRequiredService<ILogger<GroqIncidentAiAnalysisService>>();

            return new GroqIncidentAiAnalysisService(
                chatClient,
                logger,
                modelId);
        });

        return services;
    }
}
