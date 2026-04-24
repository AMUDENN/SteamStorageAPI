using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Jobs;

public sealed class TriggerJobRequestValidator : AbstractValidator<TriggerJobRequest>
{
    public TriggerJobRequestValidator()
    {
        RuleFor(expression => expression.JobName).IsInEnum().WithMessage("Job number must be in the range from 0 to 2");
    }
}