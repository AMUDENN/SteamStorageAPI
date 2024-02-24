using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.Pages;

public sealed class SetPageRequestValidator : AbstractValidator<PagesController.SetPageRequest>
{
    public SetPageRequestValidator()
    {
        RuleFor(expression => expression.PageId)
            .GreaterThan(0).WithMessage("Id страницы не может быть меньше 1")
            .LessThan(int.MaxValue).WithMessage($"Id страницы не может быть больше {int.MaxValue}");
    }
}
