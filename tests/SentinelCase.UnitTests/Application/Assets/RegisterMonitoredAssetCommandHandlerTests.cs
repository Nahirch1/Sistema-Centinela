using Microsoft.Extensions.Time.Testing;

using SentinelCase.Application.Features.Assets.Commands.RegisterMonitoredAsset;
using SentinelCase.Domain.Exceptions;
using SentinelCase.UnitTests.TestDoubles;

namespace SentinelCase.UnitTests.Application.Assets;

public sealed class RegisterMonitoredAssetCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCommand_ShouldRegisterActiveAsset()
    {
        var currentTime = new DateTimeOffset(
            2026,
            9,
            15,
            12,
            0,
            0,
            TimeSpan.Zero);

        var timeProvider = new FakeTimeProvider(currentTime);
        var repository = new FakeMonitoredAssetRepository();

        var handler = new RegisterMonitoredAssetCommandHandler(
            repository,
            timeProvider);

        var command = new RegisterMonitoredAssetCommand(
            "web-server-01");

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("web-server-01", result.Name);
        Assert.False(string.IsNullOrWhiteSpace(result.ApiKey));
        Assert.Equal(currentTime, result.RegisteredAt);

        var storedAsset = Assert.Single(repository.Assets);

        Assert.Equal(result.Id, storedAsset.Id);

        Assert.NotEqual(result.ApiKey, storedAsset.ApiKeyHash);
    }

    [Fact]
    public async Task Handle_CalledTwice_ShouldGenerateDifferentApiKeys()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var repository = new FakeMonitoredAssetRepository();

        var handler = new RegisterMonitoredAssetCommandHandler(
            repository,
            timeProvider);

        var firstResult = await handler.Handle(
            new RegisterMonitoredAssetCommand("asset-one"),
            CancellationToken.None);

        var secondResult = await handler.Handle(
            new RegisterMonitoredAssetCommand("asset-two"),
            CancellationToken.None);

        Assert.NotEqual(firstResult.ApiKey, secondResult.ApiKey);
    }

    [Fact]
    public async Task Handle_WithDuplicatedName_ShouldThrowDomainException()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var repository = new FakeMonitoredAssetRepository();

        var handler = new RegisterMonitoredAssetCommandHandler(
            repository,
            timeProvider);

        await handler.Handle(
            new RegisterMonitoredAssetCommand("web-server-01"),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(
                new RegisterMonitoredAssetCommand("web-server-01"),
                CancellationToken.None));

        Assert.Equal(
            "An asset with the same name is already registered.",
            exception.Message);

        Assert.Single(repository.Assets);
    }
}
