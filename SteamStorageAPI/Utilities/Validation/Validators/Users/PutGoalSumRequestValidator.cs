using FluentValidation;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Utilities.Validation;
using SteamStorageAPI.Utilities.Validation.Tools;

namespace SteamStorageAPI.Utilities.Validation.Validators.Users;

public sealed class PutGoalSumRequestValidator : AbstractValidator<PutGoalSumRequest>
{
    public PutGoalSumRequestValidator()
    {
        RuleFor(expression => expression.GoalSum)
            .GreaterThanOrEqualTo(0).WithMessage("The financial goal cannot be less than 0")
            .LessThan(ValidationConstants.MaxPrice).WithMessage("The financial goal cannot be greater than 999999999999");
    }
}