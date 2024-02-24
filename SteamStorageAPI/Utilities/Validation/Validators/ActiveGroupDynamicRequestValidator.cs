using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators;

public sealed class ActiveGroupDynamicRequestValidator : AbstractValidator<ActiveGroupsController.GetActiveGroupDynamicRequest>
{
    public ActiveGroupDynamicRequestValidator()
    {
        RuleFor(expression => expression.DaysDynamic).GreaterThan(0)
            .WithMessage("Количество дней не может быть меньше 1");

    }
}