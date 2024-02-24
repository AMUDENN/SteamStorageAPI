using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.ActiveGroups;

public sealed class PostActiveGroupRequestValidator : AbstractValidator<ActiveGroupsController.PostActiveGroupRequest>
{
    public PostActiveGroupRequestValidator()
    {
        RuleFor(expression => expression.Title).Length(3, 100)
            .WithMessage("Длина названия группы должна быть от 3 до 100 символов");

        RuleFor(expression => expression.Description).MaximumLength(300)
            .WithMessage("Длина описания группы должна быть от 0 до 300 символов");

        RuleFor(expression => expression.Colour)
            .Matches("^([A-Fa-f0-9]{8}|[A-Fa-f0-9]{6}|[A-Fa-f0-9]{4}|[A-Fa-f0-9]{3})$")
            .WithMessage(
                "Цвет не выполняет условия, примеры правильного указания цвета: FA12AD29, FF1AFF, 2483, AD0");

        RuleFor(expression => expression.GoalSum).GreaterThan(0)
            .WithMessage("Финансовая цель должна быть больше 0");
    }
}