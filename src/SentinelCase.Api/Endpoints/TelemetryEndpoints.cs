using System.Security.Claims;

using MediatR;

using SentinelCase.Api.Common.Authorization;
using SentinelCase.Application.Features.Telemetry.Commands.IngestSecurityEvent;
using SentinelCase.Domain.Enums;

namespace SentinelCase.Api.Endpoints;

public static class TelemetryEndpoints
{
    public static IEndpointRouteBuilder MapTelemetryEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/telemetry")
            .WithTags("Telemetry");

        group.MapPost("/events", IngestEventAsync)
            .WithName("IngestSecurityEvent")
            .RequireAuthorization(AppPolicies.RequireAssetApiKey)
            .Produces<IngestSecurityEventResult>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem();

        return endpoints;
    }

    private static async Task<IResult> IngestEventAsync(
        IngestEventRequest request,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var assetIdClaim = user.FindFirstValue(
            ClaimTypes.NameIdentifier);

        var monitoredAssetId = Guid.Parse(assetIdClaim!);

        var command = new IngestSecurityEventCommand(
            monitoredAssetId,
            request.EventType,
            request.SourceIdentifier,
            request.Message,
            request.OccurredAt);

        var result = await sender.Send(command, cancellationToken);

        return Results.Created(
            $"/api/telemetry/events/{result.Id}",
            result);
    }

    public sealed record IngestEventRequest(
        SecurityEventType EventType,
        string SourceIdentifier,
        string Message,
        DateTimeOffset OccurredAt);
}
