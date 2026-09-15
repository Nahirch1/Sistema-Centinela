namespace SentinelCase.Agent;

/// <summary>
/// Generates synthetic telemetry events at a fixed interval. Useful
/// for demos and for testing the HTTP reporting path without
/// depending on a real failed sudo/login attempt happening on the
/// host machine.
/// </summary>
public sealed class SimulatedEventSource
{
    private readonly TimeSpan _interval;
    private static readonly string[] SourceIps =
    [
        "198.51.100.23",
        "203.0.113.45",
        "192.0.2.10"
    ];

    public SimulatedEventSource(TimeSpan interval)
    {
        _interval = interval;
    }

    public async IAsyncEnumerable<TelemetryEvent> ReadAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken)
    {
        var random = new Random();

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(_interval, cancellationToken);

            var sourceIp = SourceIps[random.Next(SourceIps.Length)];

            yield return new TelemetryEvent(
                EventType: 1, // FailedLoginAttempt
                SourceIdentifier: sourceIp,
                Message: $"[SIMULATED] Failed login attempt from {sourceIp}",
                OccurredAt: DateTimeOffset.UtcNow);
        }
    }
}
