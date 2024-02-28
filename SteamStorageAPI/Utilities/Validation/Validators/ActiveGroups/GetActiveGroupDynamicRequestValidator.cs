using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.ActiveGroups;

public sealed class GetActiveGroupDynamicRequestValidator : AbstractValidator<ActiveGroupsController.GetActiveGroupDynamicRequest>
{
    public GetActiveGroupDynamicRequestValidator()
    {
        RuleFor(expression => expression.GroupId)
            .GreaterThan(0).WithMessage("Id группы не может быть меньше 1");
        
        RuleFor(expression => expression.EndDate)
            .GreaterThan(expression => expression.StartDate)
            .WithMessage("Дата конца периода должна быть больше даты начала периода");
    }
}
