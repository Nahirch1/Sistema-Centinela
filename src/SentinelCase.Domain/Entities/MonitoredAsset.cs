using SentinelCase.Domain.Common;
using SentinelCase.Domain.Enums;
using SentinelCase.Domain.Events;
using SentinelCase.Domain.Exceptions;

namespace SentinelCase.Domain.Entities;

/// <summary>
/// A team/equipment registered to send telemetry to Centinela.
/// The API key is never stored in plain text - only its hash.
/// </summary>
public sealed class MonitoredAsset : IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    private const int MaximumNameLength = 200;

    private MonitoredAsset()
    {
    }

    private MonitoredAsset(
        Guid id,
        string name,
        string apiKeyHash,
        DateTimeOffset registeredAt)
    {
        Id = id;
        Name = name;
        ApiKeyHash = apiKeyHash;
        Status = AssetStatus.Active;
        RegisteredAt = registeredAt;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string ApiKeyHash { get; private set; } = string.Empty;

    public AssetStatus Status { get; private set; }

    public DateTimeOffset RegisteredAt { get; private set; }

    public DateTimeOffset? LastSeenAt { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents =>
        _domainEvents.AsReadOnly();

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public static MonitoredAsset Create(
        string name,
        string apiKeyHash,
        DateTimeOffset registeredAt)
    {
        ValidateName(name);

        if (string.IsNullOrWhiteSpace(apiKeyHash))
        {
            throw new DomainException(
                "The API key hash is required.");
        }

        var asset = new MonitoredAsset(
            Guid.NewGuid(),
            name.Trim(),
            apiKeyHash,
            registeredAt);

        asset._domainEvents.Add(
            new AssetRegisteredDomainEvent(
                asset.Id,
                asset.Name));

        return asset;
    }

    public void RecordHeartbeat(DateTimeOffset occurredAt)
    {
        EnsureActive();

        LastSeenAt = occurredAt;
    }

    public void RevokeApiKey()
    {
        if (Status == AssetStatus.Revoked)
        {
            throw new DomainException(
                "The asset's API key is already revoked.");
        }

        Status = AssetStatus.Revoked;

        _domainEvents.Add(
            new AssetApiKeyRevokedDomainEvent(Id));
    }

    private void EnsureActive()
    {
        if (Status != AssetStatus.Active)
        {
            throw new DomainException(
                "A revoked asset cannot report telemetry.");
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException(
                "The asset name is required.");
        }

        if (name.Trim().Length > MaximumNameLength)
        {
            throw new DomainException(
                $"The asset name cannot exceed {MaximumNameLength} characters.");
        }
    }
}
