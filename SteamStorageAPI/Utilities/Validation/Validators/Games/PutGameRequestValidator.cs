using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Games;

public sealed class PutGameRequestValidator : AbstractValidator<PutGameRequest>
{
    public PutGameRequestValidator()
    {
        RuleFor(expression => expression.GameId)
            .GreaterThan(0).WithMessage("Game Id cannot be less than 1");

        RuleFor(expression => expression.Title)
            .Length(3, 300).WithMessage("The length of the game title must be between 3 and 300 characters");
    }
}