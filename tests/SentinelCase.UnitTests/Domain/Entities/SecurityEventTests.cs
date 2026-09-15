using SentinelCase.Domain.Entities;
using SentinelCase.Domain.Enums;
using SentinelCase.Domain.Exceptions;

namespace SentinelCase.UnitTests.Domain.Entities;

public sealed class SecurityEventTests
{
    private static readonly Guid ValidAssetId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldCreateEvent()
    {
        var occurredAt = new DateTimeOffset(
            2026,
            9,
            15,
            10,
            0,
            0,
            TimeSpan.Zero);

        var receivedAt = occurredAt.AddSeconds(2);

        var securityEvent = SecurityEvent.Create(
            ValidAssetId,
            SecurityEventType.FailedLoginAttempt,
            "198.51.100.23",
            "Failed login for user 'admin'.",
            occurredAt,
            receivedAt);

        Assert.NotEqual(Guid.Empty, securityEvent.Id);
        Assert.Equal(ValidAssetId, securityEvent.MonitoredAssetId);
        Assert.Equal(
            SecurityEventType.FailedLoginAttempt,
            securityEvent.EventType);
        Assert.Equal("198.51.100.23", securityEvent.SourceIdentifier);
        Assert.Equal(
            "Failed login for user 'admin'.",
            securityEvent.Message);
        Assert.Equal(occurredAt, securityEvent.OccurredAt);
        Assert.Equal(receivedAt, securityEvent.ReceivedAt);
    }

    [Fact]
    public void Create_WithEmptyAssetId_ShouldThrowDomainException()
    {
        var now = DateTimeOffset.UtcNow;

        var exception = Assert.Throws<DomainException>(() =>
            SecurityEvent.Create(
                Guid.Empty,
                SecurityEventType.FailedLoginAttempt,
                "198.51.100.23",
                "Failed login.",
                now,
                now));

        Assert.Equal(
            "The monitored asset id is required.",
            exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("     ")]
    public void Create_WithEmptySourceIdentifier_ShouldThrowDomainException(
        string sourceIdentifier)
    {
        var now = DateTimeOffset.UtcNow;

        var exception = Assert.Throws<DomainException>(() =>
            SecurityEvent.Create(
                ValidAssetId,
                SecurityEventType.FailedLoginAttempt,
                sourceIdentifier,
                "Failed login.",
                now,
                now));

        Assert.Equal(
            "The event source identifier is required.",
            exception.Message);
    }

    [Fact]
    public void Create_WithOccurredAtAfterReceivedAt_ShouldThrowDomainException()
    {
        var receivedAt = DateTimeOffset.UtcNow;
        var occurredAt = receivedAt.AddMinutes(1);

        var exception = Assert.Throws<DomainException>(() =>
            SecurityEvent.Create(
                ValidAssetId,
                SecurityEventType.FailedLoginAttempt,
                "198.51.100.23",
                "Failed login.",
                occurredAt,
                receivedAt));

        Assert.Equal(
            "The event occurrence date cannot be later than its reception date.",
            exception.Message);
    }
}
