using SentinelCase.Domain.Entities;
using SentinelCase.Domain.Enums;
using SentinelCase.Domain.Exceptions;

namespace SentinelCase.UnitTests.Domain.Entities;

public sealed class MonitoredAssetTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateActiveAsset()
    {
        var registeredAt = new DateTimeOffset(
            2026,
            9,
            15,
            10,
            0,
            0,
            TimeSpan.Zero);

        var asset = MonitoredAsset.Create(
            "web-server-01",
            "hashed-api-key-value",
            registeredAt);

        Assert.NotEqual(Guid.Empty, asset.Id);
        Assert.Equal("web-server-01", asset.Name);
        Assert.Equal("hashed-api-key-value", asset.ApiKeyHash);
        Assert.Equal(AssetStatus.Active, asset.Status);
        Assert.Equal(registeredAt, asset.RegisteredAt);
        Assert.Null(asset.LastSeenAt);
    }

    [Fact]
    public void Create_WithWhitespaceAroundName_ShouldTrimIt()
    {
        var now = DateTimeOffset.UtcNow;

        var asset = MonitoredAsset.Create(
            "  db-server-02  ",
            "hashed-api-key-value",
            now);

        Assert.Equal("db-server-02", asset.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("     ")]
    public void Create_WithEmptyName_ShouldThrowDomainException(string name)
    {
        var now = DateTimeOffset.UtcNow;

        var exception = Assert.Throws<DomainException>(() =>
            MonitoredAsset.Create(
                name,
                "hashed-api-key-value",
                now));

        Assert.Equal(
            "The asset name is required.",
            exception.Message);
    }

    [Fact]
    public void Create_WithEmptyApiKeyHash_ShouldThrowDomainException()
    {
        var now = DateTimeOffset.UtcNow;

        var exception = Assert.Throws<DomainException>(() =>
            MonitoredAsset.Create(
                "web-server-01",
                string.Empty,
                now));

        Assert.Equal(
            "The API key hash is required.",
            exception.Message);
    }

    [Fact]
    public void RecordHeartbeat_OnActiveAsset_ShouldUpdateLastSeenAt()
    {
        var registeredAt = DateTimeOffset.UtcNow;
        var heartbeatAt = registeredAt.AddMinutes(5);

        var asset = MonitoredAsset.Create(
            "web-server-01",
            "hashed-api-key-value",
            registeredAt);

        asset.RecordHeartbeat(heartbeatAt);

        Assert.Equal(heartbeatAt, asset.LastSeenAt);
    }

    [Fact]
    public void RecordHeartbeat_OnRevokedAsset_ShouldThrowDomainException()
    {
        var now = DateTimeOffset.UtcNow;

        var asset = MonitoredAsset.Create(
            "web-server-01",
            "hashed-api-key-value",
            now);

        asset.RevokeApiKey();

        var exception = Assert.Throws<DomainException>(() =>
            asset.RecordHeartbeat(now.AddMinutes(1)));

        Assert.Equal(
            "A revoked asset cannot report telemetry.",
            exception.Message);
    }

    [Fact]
    public void RevokeApiKey_WhenAlreadyRevoked_ShouldThrowDomainException()
    {
        var now = DateTimeOffset.UtcNow;

        var asset = MonitoredAsset.Create(
            "web-server-01",
            "hashed-api-key-value",
            now);

        asset.RevokeApiKey();

        var exception = Assert.Throws<DomainException>(
            asset.RevokeApiKey);

        Assert.Equal(
            "The asset's API key is already revoked.",
            exception.Message);
    }
}
