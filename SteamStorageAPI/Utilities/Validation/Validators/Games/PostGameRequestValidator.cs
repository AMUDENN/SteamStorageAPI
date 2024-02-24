using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.Games;

public sealed class PostGameRequestValidator : AbstractValidator<GamesController.PostGameRequest>
{
    public PostGameRequestValidator()
    {
        RuleFor(expression => expression.SteamGameId)
            .GreaterThan(0).WithMessage("Id игры не может быть меньше 1")
            .LessThan(int.MaxValue).WithMessage($"Id игры не может быть больше {int.MaxValue}");
    }
}
