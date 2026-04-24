using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Games;

public sealed class PostGameRequestValidator : AbstractValidator<PostGameRequest>
{
    public PostGameRequestValidator()
    {
        RuleFor(expression => expression.SteamGameId)
            .GreaterThan(0).WithMessage("Game Id cannot be less than 1");
    }
}