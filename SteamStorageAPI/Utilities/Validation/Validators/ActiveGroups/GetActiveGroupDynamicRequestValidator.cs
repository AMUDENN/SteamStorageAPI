using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.ActiveGroups;

public sealed class GetActiveGroupDynamicRequestValidator : AbstractValidator<GetActiveGroupDynamicRequest>
{
    public GetActiveGroupDynamicRequestValidator()
    {
        RuleFor(expression => expression.GroupId)
            .GreaterThan(0).WithMessage("Group Id cannot be less than 1");

        RuleFor(expression => expression.EndDate)
            .GreaterThan(expression => expression.StartDate)
            .WithMessage("The end date of the period must be greater than the start date of the period");
    }
}