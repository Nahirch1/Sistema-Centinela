using System.Text.Json;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using SentinelCase.Domain.Common;
using SentinelCase.Domain.Entities;
using SentinelCase.Infrastructure.Messaging.Outbox;
using SentinelCase.Infrastructure.Identity;

namespace SentinelCase.Infrastructure.Persistence;

public sealed class ApplicationDbContext
    : IdentityDbContext<
        ApplicationUser,
        IdentityRole<Guid>,
        Guid>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<SecurityIncident> SecurityIncidents =>
        Set<SecurityIncident>();

    public DbSet<IncidentHistoryEntry> IncidentHistoryEntries =>
        Set<IncidentHistoryEntry>();

    public DbSet<IncidentNote> IncidentNotes =>
        Set<IncidentNote>();

    public DbSet<RefreshToken> RefreshTokens =>
        Set<RefreshToken>();

    public DbSet<OutboxMessage> OutboxMessages =>
        Set<OutboxMessage>();

    public DbSet<MonitoredAsset> MonitoredAssets =>
        Set<MonitoredAsset>();

    public DbSet<SecurityEvent> SecurityEvents =>
        Set<SecurityEvent>();

    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        AddDomainEventsToOutbox();

        return base.SaveChangesAsync(
            cancellationToken);
    }

    private void AddDomainEventsToOutbox()
    {
        var entities = ChangeTracker
            .Entries<IHasDomainEvents>()
            .Select(entry => entry.Entity)
            .Where(entity => entity.DomainEvents.Count > 0)
            .ToArray();

        if (entities.Length == 0)
        {
            return;
        }

        var messages = entities
            .SelectMany(entity =>
                entity.DomainEvents.Select(domainEvent =>
                    new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        Type = domainEvent.GetType().FullName
                            ?? domainEvent.GetType().Name,
                        Payload = JsonSerializer.Serialize(
                            domainEvent,
                            domainEvent.GetType()),
                        OccurredAt = DateTimeOffset.UtcNow
                    }))
            .ToArray();

        OutboxMessages.AddRange(messages);

        foreach (var entity in entities)
        {
            entity.ClearDomainEvents();
        }
    }

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }
}
