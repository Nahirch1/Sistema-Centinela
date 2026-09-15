using FluentValidation;

namespace SentinelCase.Application.Features.Telemetry.Commands.IngestSecurityEvent;

public sealed class IngestSecurityEventCommandValidator
    : AbstractValidator<IngestSecurityEventCommand>
{
    public IngestSecurityEventCommandValidator()
    {
        RuleFor(command => command.MonitoredAssetId)
            .NotEmpty();

        RuleFor(command => command.EventType)
            .IsInEnum();

        RuleFor(command => command.SourceIdentifier)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.Message)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(command => command.OccurredAt)
            .NotEmpty();
    }
}
