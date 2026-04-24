using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.ArchiveGroups;

public sealed class DeleteArchiveGroupRequestValidator : AbstractValidator<DeleteArchiveGroupRequest>
{
    public DeleteArchiveGroupRequestValidator()
    {
        RuleFor(expression => expression.GroupId)
            .GreaterThan(0).WithMessage("Group Id cannot be less than 1");
    }
}