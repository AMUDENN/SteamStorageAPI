using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.ArchiveGroups;

public sealed class GetArchiveGroupInfoRequestValidator : AbstractValidator<GetArchiveGroupInfoRequest>
{
    public GetArchiveGroupInfoRequestValidator()
    {
        RuleFor(expression => expression.GroupId)
            .GreaterThan(0).WithMessage("Id группы не может быть меньше 1");
    }
}