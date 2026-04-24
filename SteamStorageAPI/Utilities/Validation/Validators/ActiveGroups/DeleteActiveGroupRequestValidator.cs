using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.ActiveGroups;

public sealed class DeleteActiveGroupRequestValidator : AbstractValidator<DeleteActiveGroupRequest>
{
    public DeleteActiveGroupRequestValidator()
    {
        RuleFor(expression => expression.GroupId)
            .GreaterThan(0).WithMessage("Group Id cannot be less than 1");
    }
}