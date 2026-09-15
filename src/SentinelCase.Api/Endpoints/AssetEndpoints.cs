using MediatR;

using SentinelCase.Api.Common.Authorization;
using SentinelCase.Application.Features.Assets.Commands.RegisterMonitoredAsset;

namespace SentinelCase.Api.Endpoints;

public static class AssetEndpoints
{
    public static IEndpointRouteBuilder MapAssetEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/assets")
            .WithTags("Assets");

        group.MapPost("/", RegisterAssetAsync)
            .WithName("RegisterMonitoredAsset")
            .RequireAuthorization(AppPolicies.CanCreateIncident)
            .Produces<RegisterMonitoredAssetResult>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem();

        return endpoints;
    }

    private static async Task<IResult> RegisterAssetAsync(
        RegisterAssetRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new RegisterMonitoredAssetCommand(
            request.Name);

        var result = await sender.Send(command, cancellationToken);

        return Results.Created(
            $"/api/assets/{result.Id}",
            result);
    }

    public sealed record RegisterAssetRequest(
        string Name);
}
