using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.Users;

public sealed class PutStartPageRequestValidator : AbstractValidator<UsersController.PutStartPageRequest>
{
    public PutStartPageRequestValidator()
    {
        RuleFor(expression => expression.StartPageId)
            .GreaterThan(0).WithMessage("Id страницы не может быть меньше 1")
            .LessThan(int.MaxValue).WithMessage($"Id страницы не может быть больше {int.MaxValue}");
    }
}
