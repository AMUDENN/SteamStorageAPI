using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.Actives;

public sealed class SoldActiveRequestValidator : AbstractValidator<ActivesController.SoldActiveRequest>
{
    public SoldActiveRequestValidator()
    {
        RuleFor(expression => expression.Id)
            .GreaterThan(0).WithMessage("Id актива не может быть меньше 1");
        
        RuleFor(expression => expression.GroupId)
            .GreaterThan(0).WithMessage("Id группы не может быть меньше 1");

        RuleFor(expression => expression.Count)
            .GreaterThan(0).WithMessage("Количество предметов не может быть меньше 1");

        RuleFor(expression => expression.SoldPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Стоимость покупки не может быть меньше 0")
            .LessThan(1000000000000).WithMessage("Стоимость покупки не может быть больше 999999999999");

        RuleFor(expression => expression.Description)
            .MaximumLength(300).WithMessage("Длина описания предмета должна быть от 0 до 300 символов");
    }
}
