using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.Users;

public sealed class GetUserRequestValidator : AbstractValidator<UsersController.GetUserRequest>
{
    public GetUserRequestValidator()
    {
        RuleFor(expression => expression.UserId)
            .GreaterThan(0).WithMessage("Id пользователя не может быть меньше 1")
            .LessThan(int.MaxValue).WithMessage($"Id пользователя не может быть больше {int.MaxValue}");
    }
}
