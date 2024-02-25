using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.ActiveGroups;

public sealed class DeleteActiveGroupRequestValidator : AbstractValidator<ActiveGroupsController.DeleteActiveGroupRequest>
{
    public DeleteActiveGroupRequestValidator()
    {
        RuleFor(expression => expression.GroupId)
            .GreaterThan(0).WithMessage("Id группы не может быть меньше 1");
    }
}
