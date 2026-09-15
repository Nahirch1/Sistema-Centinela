using MediatR;

namespace SentinelCase.Application.Features.Assets.Commands.RegisterMonitoredAsset;

public sealed record RegisterMonitoredAssetCommand(
    string Name) : IRequest<RegisterMonitoredAssetResult>;
