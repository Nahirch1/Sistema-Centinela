using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SentinelCase.Agent;

/// <summary>
/// Follows `journalctl _COMM=sudo -f` and yields a TelemetryEvent for
/// every "incorrect password attempt" line sudo logs on a failed
/// sudo attempt. This is real telemetry from the local machine, not
/// synthetic data.
/// </summary>
public sealed partial class JournalSudoAuthSource
{
    public async IAsyncEnumerable<TelemetryEvent> ReadAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "journalctl",
            ArgumentList = { "_COMM=sudo", "-f", "-n", "0", "--no-pager" },
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException(
                "Could not start journalctl. Is systemd-journal available?");

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await process.StandardOutput.ReadLineAsync(
                cancellationToken);

            if (line is null)
            {
                yield break;
            }

            if (!line.Contains("incorrect password attempt"))
            {
                continue;
            }

            var userMatch = IncorrectAttemptUserRegex().Match(line);

            var user = userMatch.Success
                ? userMatch.Groups[1].Value
                : "unknown";

            yield return new TelemetryEvent(
                EventType: 1, // FailedLoginAttempt
                SourceIdentifier: user,
                Message: line.Trim(),
                OccurredAt: DateTimeOffset.UtcNow);
        }
    }

    [GeneratedRegex(@"^\S+\s+\S+\s+[\d:]+\s+\S+\s+sudo\[\d+\]:\s+(\S+)\s+:")]
    private static partial Regex IncorrectAttemptUserRegex();
}
