using FluentValidation;

namespace SentinelCase.Application.Features.Assets.Commands.RegisterMonitoredAsset;

public sealed class RegisterMonitoredAssetCommandValidator
    : AbstractValidator<RegisterMonitoredAssetCommand>
{
    public RegisterMonitoredAssetCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}
