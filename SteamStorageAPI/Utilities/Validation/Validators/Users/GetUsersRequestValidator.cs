using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.Users;

public sealed class GetUsersRequestValidator : AbstractValidator<UsersController.GetUsersRequest>
{
    public GetUsersRequestValidator()
    {
        RuleFor(expression => expression.PageNumber)
            .GreaterThan(0).WithMessage("Номер страницы не может быть меньше 1");

        RuleFor(expression => expression.PageSize)
            .InclusiveBetween(1, 200).WithMessage("Размер страницы должен находиться в интервале от 1 до 200");
    }
}
