using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelCase.Domain.Entities;

namespace SentinelCase.Infrastructure.Persistence.Configurations;

internal sealed class SecurityEventConfiguration
    : IEntityTypeConfiguration<SecurityEvent>
{
    public void Configure(
        EntityTypeBuilder<SecurityEvent> builder)
    {
        builder.ToTable("SecurityEvents");

        builder.HasKey(securityEvent => securityEvent.Id);

        builder.Property(securityEvent => securityEvent.Id)
            .ValueGeneratedNever();

        builder.Property(securityEvent => securityEvent.MonitoredAssetId)
            .IsRequired();

        builder.Property(securityEvent => securityEvent.EventType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(securityEvent => securityEvent.SourceIdentifier)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(securityEvent => securityEvent.Message)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(securityEvent => securityEvent.OccurredAt)
            .IsRequired();

        builder.Property(securityEvent => securityEvent.ReceivedAt)
            .IsRequired();

        builder.HasOne<MonitoredAsset>()
            .WithMany()
            .HasForeignKey(securityEvent => securityEvent.MonitoredAssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(securityEvent => securityEvent.MonitoredAssetId);

        builder.HasIndex(securityEvent => securityEvent.EventType);

        builder.HasIndex(securityEvent => securityEvent.OccurredAt);
    }
}
