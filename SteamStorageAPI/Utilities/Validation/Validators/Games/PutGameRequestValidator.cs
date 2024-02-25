using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.Games;

public sealed class PutGameRequestValidator : AbstractValidator<GamesController.PutGameRequest>
{
    public PutGameRequestValidator()
    {
        RuleFor(expression => expression.GameId)
            .GreaterThan(0).WithMessage("Id игры не может быть меньше 1");
        
        RuleFor(expression => expression.Title)
            .Length(3, 300).WithMessage("Длина названия игры должна быть от 3 до 300 символов");
    }
}
