using SentinelCase.Domain.Common;
using SentinelCase.Domain.Enums;
using SentinelCase.Domain.Events;
using SentinelCase.Domain.Exceptions;

namespace SentinelCase.Domain.Entities;

public sealed class SecurityIncident : IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    private const int MaximumTitleLength = 200;
    private const int MaximumDescriptionLength = 4000;

    private SecurityIncident()
    {
    }

    private SecurityIncident(
        Guid id,
        string title,
        string description,
        IncidentSeverity severity,
        DateTimeOffset detectedAt,
        DateTimeOffset createdAt)
    {
        Id = id;
        Title = title;
        Description = description;
        Severity = severity;
        Status = IncidentStatus.Open;
        DetectedAt = detectedAt;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public IncidentSeverity Severity { get; private set; }

    public IncidentStatus Status { get; private set; }

    public DateTimeOffset DetectedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ClosedAt { get; private set; }

    public string? AssignedTo { get; private set; }

    public DateTimeOffset? AssignedAt { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents =>
        _domainEvents.AsReadOnly();

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public static SecurityIncident Create(
        string title,
        string description,
        IncidentSeverity severity,
        DateTimeOffset detectedAt,
        DateTimeOffset createdAt)
    {
        ValidateTitle(title);
        ValidateDescription(description);
        ValidateSeverity(severity);

        if (detectedAt > createdAt)
        {
            throw new DomainException(
                "The incident detection date cannot be later than its creation date.");
        }

        var incident = new SecurityIncident(
            Guid.NewGuid(),
            title.Trim(),
            description.Trim(),
            severity,
            detectedAt,
            createdAt);

        incident._domainEvents.Add(
            new IncidentCreatedDomainEvent(
                incident.Id,
                incident.Title,
                incident.Severity));

        return incident;
    }

    public void StartInvestigation()
    {
        EnsureNotClosed();

        if (Status != IncidentStatus.Open)
        {
            throw new DomainException(
                "Only an open incident can enter investigation.");
        }

        var previousStatus = Status;

        Status = IncidentStatus.UnderInvestigation;

        _domainEvents.Add(
            new IncidentStatusChangedDomainEvent(
                Id,
                previousStatus,
                Status));
    }

    public void Contain()
    {
        EnsureNotClosed();

        if (Status != IncidentStatus.UnderInvestigation)
        {
            throw new DomainException(
                "Only an incident under investigation can be contained.");
        }

        var previousStatus = Status;

        Status = IncidentStatus.Contained;

        _domainEvents.Add(
            new IncidentStatusChangedDomainEvent(
                Id,
                previousStatus,
                Status));
    }

    public void Resolve()
    {
        EnsureNotClosed();

        if (Status != IncidentStatus.Contained)
        {
            throw new DomainException(
                "Only a contained incident can be resolved.");
        }

        var previousStatus = Status;

        Status = IncidentStatus.Resolved;

        _domainEvents.Add(
            new IncidentStatusChangedDomainEvent(
                Id,
                previousStatus,
                Status));
    }

    public void Close(DateTimeOffset closedAt)
    {
        EnsureNotClosed();

        if (Status != IncidentStatus.Resolved)
        {
            throw new DomainException(
                "Only a resolved incident can be closed.");
        }

        if (closedAt < CreatedAt)
        {
            throw new DomainException(
                "The closure date cannot be earlier than the creation date.");
        }

        var previousStatus = Status;

        Status = IncidentStatus.Closed;
        ClosedAt = closedAt;

        _domainEvents.Add(
            new IncidentStatusChangedDomainEvent(
                Id,
                previousStatus,
                Status));
    }

    public void AssignTo(
        string analystIdentifier,
        DateTimeOffset assignedAt)
    {
        EnsureNotClosed();

        if (string.IsNullOrWhiteSpace(analystIdentifier))
        {
            throw new DomainException(
                "The analyst identifier is required.");
        }

        if (analystIdentifier.Trim().Length > 200)
        {
            throw new DomainException(
                "The analyst identifier cannot exceed 200 characters.");
        }

        if (assignedAt < CreatedAt)
        {
            throw new DomainException(
                "The assignment date cannot be earlier than the creation date.");
        }

        AssignedTo = analystIdentifier.Trim();
        AssignedAt = assignedAt;

        _domainEvents.Add(
            new IncidentAssignedDomainEvent(
                Id,
                AssignedTo));
    }

    public void ChangeSeverity(IncidentSeverity severity)
    {
        EnsureNotClosed();
        ValidateSeverity(severity);

        var previousSeverity = Severity;

        Severity = severity;

        _domainEvents.Add(
            new IncidentSeverityChangedDomainEvent(
                Id,
                previousSeverity,
                Severity));
    }

    public void UpdateDetails(string title, string description)
    {
        EnsureNotClosed();
        ValidateTitle(title);
        ValidateDescription(description);

        Title = title.Trim();
        Description = description.Trim();

        _domainEvents.Add(
            new IncidentDetailsUpdatedDomainEvent(Id));
    }

    private void EnsureNotClosed()
    {
        if (Status == IncidentStatus.Closed)
        {
            throw new DomainException(
                "A closed incident cannot be modified.");
        }
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException(
                "The incident title is required.");
        }

        if (title.Trim().Length > MaximumTitleLength)
        {
            throw new DomainException(
                $"The incident title cannot exceed {MaximumTitleLength} characters.");
        }
    }

    private static void ValidateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException(
                "The incident description is required.");
        }

        if (description.Trim().Length > MaximumDescriptionLength)
        {
            throw new DomainException(
                $"The incident description cannot exceed {MaximumDescriptionLength} characters.");
        }
    }

    private static void ValidateSeverity(IncidentSeverity severity)
    {
        if (!Enum.IsDefined(severity))
        {
            throw new DomainException(
                "The incident severity is invalid.");
        }
    }
}