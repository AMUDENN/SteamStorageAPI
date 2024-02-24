using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.Actives;

public sealed class GetActivesCountRequestValidator : AbstractValidator<ActivesController.GetActivesCountRequest>
{
    public GetActivesCountRequestValidator()
    {
        RuleFor(expression => expression.GroupId)
            .GreaterThan(0).WithMessage("Id группы не может быть меньше 1")
            .LessThan(int.MaxValue).WithMessage($"Id группы не может быть больше {int.MaxValue}");

        RuleFor(expression => expression.GameId)
            .GreaterThan(0).WithMessage("Id игры не может быть меньше 1")
            .LessThan(int.MaxValue).WithMessage($"Id игры не может быть больше {int.MaxValue}");
    }
}
