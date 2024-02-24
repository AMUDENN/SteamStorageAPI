using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.ArchiveGroups;

public sealed class DeleteArchiveGroupRequestValidator : AbstractValidator<ArchiveGroupsController.DeleteArchiveGroupRequest>
{
    public DeleteArchiveGroupRequestValidator()
    {
        RuleFor(expression => expression.GroupId)
            .GreaterThan(0).WithMessage("Id группы не может быть меньше 1")
            .LessThan(int.MaxValue).WithMessage($"Id группы не может быть больше {int.MaxValue}");
    }
}
