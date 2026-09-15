using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelCase.Domain.Entities;

namespace SentinelCase.Infrastructure.Persistence.Configurations;

internal sealed class MonitoredAssetConfiguration
    : IEntityTypeConfiguration<MonitoredAsset>
{
    public void Configure(
        EntityTypeBuilder<MonitoredAsset> builder)
    {
        builder.ToTable("MonitoredAssets");

        builder.HasKey(asset => asset.Id);

        builder.Property(asset => asset.Id)
            .ValueGeneratedNever();

        builder.Property(asset => asset.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(asset => asset.ApiKeyHash)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(asset => asset.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(asset => asset.RegisteredAt)
            .IsRequired();

        builder.Property(asset => asset.LastSeenAt);

        builder.HasIndex(asset => asset.Name)
            .IsUnique();

        builder.HasIndex(asset => asset.ApiKeyHash)
            .IsUnique();

        builder.HasIndex(asset => asset.Status);
    }
}
