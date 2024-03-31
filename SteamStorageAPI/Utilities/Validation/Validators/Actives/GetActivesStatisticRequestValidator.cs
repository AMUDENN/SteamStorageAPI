using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.Actives;

public sealed class GetActivesStatisticRequestValidator : AbstractValidator<ActivesController.GetActivesStatisticRequest>
{
    public GetActivesStatisticRequestValidator()
    {
        RuleFor(expression => expression.GroupId)
            .GreaterThan(0).WithMessage("Id группы не может быть меньше 1");

        RuleFor(expression => expression.GameId)
            .GreaterThan(0).WithMessage("Id игры не может быть меньше 1");
    }
}
