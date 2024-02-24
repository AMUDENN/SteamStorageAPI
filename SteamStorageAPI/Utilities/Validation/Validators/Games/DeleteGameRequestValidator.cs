using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.Games;

public sealed class DeleteGameRequestValidator : AbstractValidator<GamesController.DeleteGameRequest>
{
    public DeleteGameRequestValidator()
    {
        RuleFor(expression => expression.GameId)
            .GreaterThan(0).WithMessage("Id игры не может быть меньше 1")
            .LessThan(int.MaxValue).WithMessage($"Id игры не может быть больше {int.MaxValue}");
    }
}
