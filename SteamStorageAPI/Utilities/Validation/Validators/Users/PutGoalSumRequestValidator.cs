using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.Users;

public sealed class PutGoalSumRequestValidator : AbstractValidator<UsersController.PutGoalSumRequest>
{
    public PutGoalSumRequestValidator()
    {
        RuleFor(expression => expression.GoalSum)
            .GreaterThanOrEqualTo(0).WithMessage("Финансовая цель не может быть меньше 0")
            .LessThan(1000000000000).WithMessage("Финансовая цель не может быть больше 999999999999");
    }
}
