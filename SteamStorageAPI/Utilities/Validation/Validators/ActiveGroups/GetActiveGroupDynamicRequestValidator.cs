using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.ActiveGroups;

public sealed class GetActiveGroupDynamicRequestValidator : AbstractValidator<ActiveGroupsController.GetActiveGroupDynamicRequest>
{
    public GetActiveGroupDynamicRequestValidator()
    {
        RuleFor(expression => expression.GroupId).GreaterThan(0)
            .WithMessage("Id группы не может быть меньше 1");
        
        RuleFor(expression => expression.DaysDynamic).GreaterThan(0)
            .WithMessage("Количество дней не может быть меньше 1");

    }
}