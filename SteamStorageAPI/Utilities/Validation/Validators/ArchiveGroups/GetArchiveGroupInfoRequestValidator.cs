using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.ArchiveGroups;

public sealed class GetArchiveGroupInfoRequestValidator : AbstractValidator<ArchiveGroupsController.GetArchiveGroupInfoRequest>
{
    public GetArchiveGroupInfoRequestValidator()
    {
        RuleFor(expression => expression.GroupId)
            .GreaterThan(0).WithMessage("Id группы не может быть меньше 1");
    }
}
