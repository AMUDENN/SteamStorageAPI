using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Roles;

public sealed class SetRoleRequestValidator : AbstractValidator<SetRoleRequest>
{
    public SetRoleRequestValidator()
    {
        RuleFor(expression => expression.UserId)
            .GreaterThan(0).WithMessage("User Id cannot be less than 1");

        RuleFor(expression => expression.RoleId)
            .GreaterThan(0).WithMessage("Role Id cannot be less than 1");
    }
}