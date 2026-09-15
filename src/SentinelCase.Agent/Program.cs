using System.Net.Http.Json;

using Microsoft.Extensions.Configuration;

using SentinelCase.Agent;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

var apiBaseUrl =
    configuration["Agent:ApiBaseUrl"]
    ?? throw new InvalidOperationException(
        "Agent:ApiBaseUrl is missing in appsettings.json.");

var apiKey =
    Environment.GetEnvironmentVariable("CENTINELA_AGENT_API_KEY")
    ?? throw new InvalidOperationException(
        "CENTINELA_AGENT_API_KEY environment variable is missing. " +
        "Register an asset via POST /api/assets and export its apiKey.");

var simulate = args.Contains("--simulate");

using var httpClientHandler = new HttpClientHandler
{
    // Local development only: the API runs with a self-signed
    // dev certificate. Never do this against a real deployment.
    ServerCertificateCustomValidationCallback =
        (_, _, _, _) => true
};

using var httpClient = new HttpClient(httpClientHandler)
{
    BaseAddress = new Uri(apiBaseUrl)
};

httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

using var cancellationSource = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationSource.Cancel();
};

Console.WriteLine("Sistema Centinela Agent starting...");
Console.WriteLine($"Reporting to: {apiBaseUrl}");
Console.WriteLine(
    simulate
        ? "Mode: SIMULATED events every 5s."
        : "Mode: REAL sudo authentication attempts (journalctl).");
Console.WriteLine("Ctrl+C to stop.");

IAsyncEnumerable<TelemetryEvent> events = simulate
    ? new SimulatedEventSource(TimeSpan.FromSeconds(5)).ReadAsync(
        cancellationSource.Token)
    : new JournalSudoAuthSource().ReadAsync(
        cancellationSource.Token);

try
{
    await foreach (var telemetryEvent in events)
    {
        Console.WriteLine(
            $"[{telemetryEvent.OccurredAt:HH:mm:ss}] Detected: {telemetryEvent.Message}");

        try
        {
            var response = await httpClient.PostAsJsonAsync(
                "/api/telemetry/events",
                new
                {
                    eventType = telemetryEvent.EventType,
                    sourceIdentifier = telemetryEvent.SourceIdentifier,
                    message = telemetryEvent.Message,
                    occurredAt = telemetryEvent.OccurredAt
                },
                cancellationSource.Token);

            Console.WriteLine(
                response.IsSuccessStatusCode
                    ? $"  -> reported ({(int)response.StatusCode})"
                    : $"  -> FAILED to report ({(int)response.StatusCode})");
        }
        catch (HttpRequestException exception)
        {
            Console.WriteLine($"  -> FAILED to report: {exception.Message}");
        }
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Agent stopped.");
}
