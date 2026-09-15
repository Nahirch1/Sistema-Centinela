using MediatR;

using SentinelCase.Api.Common.Authorization;
using SentinelCase.Application.Common.Models;
using SentinelCase.Application.Features.Incidents.Commands.AddIncidentNote;
using SentinelCase.Application.Features.Incidents.Commands.AssignIncident;
using SentinelCase.Application.Features.Incidents.Commands.ChangeIncidentStatus;
using SentinelCase.Application.Features.Incidents.Commands.CreateIncident;
using SentinelCase.Application.Features.Incidents.Commands.UpdateIncident;
using SentinelCase.Application.Features.Incidents.Queries.AnalyzeIncidentWithAi;
using SentinelCase.Application.Features.Incidents.Queries.GetIncidentById;
using SentinelCase.Application.Features.Incidents.Queries.GetIncidentHistory;
using SentinelCase.Application.Features.Incidents.Queries.GetIncidentNotes;
using SentinelCase.Application.Features.Incidents.Queries.GetIncidents;
using SentinelCase.Application.Features.Incidents.Queries.GetIncidentSummary;
using SentinelCase.Domain.Enums;

namespace SentinelCase.Api.Endpoints;

public static class IncidentEndpoints
{
    public static IEndpointRouteBuilder MapIncidentEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/incidents")
            .WithTags("Incidents");

        group.MapPost("/", CreateIncidentAsync)
            .WithName("CreateIncident")
            .RequireAuthorization(AppPolicies.CanCreateIncident)
            .Produces<CreateIncidentResult>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem();

        group.MapGet("/", GetIncidentsAsync)
            .WithName("GetIncidents")
            .RequireAuthorization()
            .Produces<PagedResult<GetIncidentsItem>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem();

        group.MapGet("/summary", GetIncidentSummaryAsync)
            .WithName("GetIncidentSummary")
            .RequireAuthorization()
            .Produces<IncidentSummary>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:guid}", GetIncidentByIdAsync)
            .WithName("GetIncidentById")
            .RequireAuthorization()
            .Produces<GetIncidentByIdResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/history", GetIncidentHistoryAsync)
            .WithName("GetIncidentHistory")
            .RequireAuthorization()
            .Produces<IReadOnlyCollection<GetIncidentHistoryItem>>(
                StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/notes", AddIncidentNoteAsync)
            .WithName("AddIncidentNote")
            .RequireAuthorization()
            .Produces<AddIncidentNoteResult>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        group.MapGet("/{id:guid}/notes", GetIncidentNotesAsync)
            .WithName("GetIncidentNotes")
            .RequireAuthorization()
            .Produces<IReadOnlyCollection<GetIncidentNoteItem>>(
                StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateIncidentAsync)
            .WithName("UpdateIncident")
            .RequireAuthorization(AppPolicies.CanCreateIncident)
            .Produces<UpdateIncidentResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        group.MapPatch("/{id:guid}/assignment", AssignIncidentAsync)
            .WithName("AssignIncident")
            .RequireAuthorization(AppPolicies.CanAssignIncident)
            .Produces<AssignIncidentResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        group.MapPatch("/{id:guid}/status", ChangeIncidentStatusAsync)
            .WithName("ChangeIncidentStatus")
            .RequireAuthorization(AppPolicies.CanManageIncidentStatus)
            .Produces<ChangeIncidentStatusResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        group.MapPost("/{id:guid}/ai/analyze", AnalyzeIncidentWithAiAsync)
            .WithName("AnalyzeIncidentWithAi")
            .RequireAuthorization()
            .Produces<AnalyzeIncidentWithAiResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> CreateIncidentAsync(
        CreateIncidentRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new CreateIncidentCommand(
            request.Title,
            request.Description,
            request.Severity,
            request.DetectedAt);

        var result = await sender.Send(command, cancellationToken);

        return Results.Created(
            $"/api/incidents/{result.Id}",
            result);
    }

    private static async Task<IResult> GetIncidentsAsync(
        int pageNumber,
        int pageSize,
        IncidentStatus? status,
        IncidentSeverity? severity,
        string? searchTerm,
        string? assignedTo,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetIncidentsQuery(
            pageNumber,
            pageSize,
            status,
            severity,
            searchTerm,
            assignedTo);

        var result = await sender.Send(
            query,
            cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> GetIncidentByIdAsync(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetIncidentByIdQuery(id);

        var result = await sender.Send(
            query,
            cancellationToken);

        return result is null
            ? Results.NotFound()
            : Results.Ok(result);
    }

    private static async Task<IResult> GetIncidentHistoryAsync(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetIncidentHistoryQuery(id);

        var result = await sender.Send(
            query,
            cancellationToken);

        return result is null
            ? Results.NotFound()
            : Results.Ok(result);
    }

    private static async Task<IResult> AddIncidentNoteAsync(
        Guid id,
        AddIncidentNoteRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new AddIncidentNoteCommand(
            id,
            request.Content);

        var result = await sender.Send(
            command,
            cancellationToken);

        return result is null
            ? Results.NotFound()
            : Results.Created(
                $"/api/incidents/{id}/notes/{result.Id}",
                result);
    }

    private static async Task<IResult> GetIncidentNotesAsync(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetIncidentNotesQuery(id);

        var result = await sender.Send(
            query,
            cancellationToken);

        return result is null
            ? Results.NotFound()
            : Results.Ok(result);
    }

    private static async Task<IResult> UpdateIncidentAsync(
        Guid id,
        UpdateIncidentRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new UpdateIncidentCommand(
            id,
            request.Title,
            request.Description,
            request.Severity);

        var result = await sender.Send(
            command,
            cancellationToken);

        return result is null
            ? Results.NotFound()
            : Results.Ok(result);
    }

    private static async Task<IResult> AssignIncidentAsync(
        Guid id,
        AssignIncidentRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new AssignIncidentCommand(
            id,
            request.AnalystIdentifier);

        var result = await sender.Send(
            command,
            cancellationToken);

        return result is null
            ? Results.NotFound()
            : Results.Ok(result);
    }

    private static async Task<IResult> ChangeIncidentStatusAsync(
        Guid id,
        ChangeIncidentStatusRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new ChangeIncidentStatusCommand(
            id,
            request.Status);

        var result = await sender.Send(
            command,
            cancellationToken);

        return result is null
            ? Results.NotFound()
            : Results.Ok(result);
    }

    private static async Task<IResult> AnalyzeIncidentWithAiAsync(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new AnalyzeIncidentWithAiQuery(id);

        var result = await sender.Send(
            query,
            cancellationToken);

        return result is null
            ? Results.NotFound()
            : Results.Ok(result);
    }

    public sealed record CreateIncidentRequest(
        string Title,
        string Description,
        IncidentSeverity Severity,
        DateTimeOffset DetectedAt);

    public sealed record UpdateIncidentRequest(
        string Title,
        string Description,
        IncidentSeverity Severity);

    public sealed record AddIncidentNoteRequest(
        string Content);

    public sealed record AssignIncidentRequest(
        string AnalystIdentifier);

    public sealed record ChangeIncidentStatusRequest(
        IncidentStatus Status);
    private static async Task<IResult> GetIncidentSummaryAsync(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetIncidentSummaryQuery(),
            cancellationToken);

        return Results.Ok(result);
    }

}
