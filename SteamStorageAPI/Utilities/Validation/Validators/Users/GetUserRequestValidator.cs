using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Users;

public sealed class GetUserRequestValidator : AbstractValidator<GetUserRequest>
{
    public GetUserRequestValidator()
    {
        RuleFor(expression => expression.UserId)
            .GreaterThan(0).WithMessage("User Id cannot be less than 1");
    }
}