using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Skins;

public sealed class PostSkinRequestValidator : AbstractValidator<PostSkinRequest>
{
    public PostSkinRequestValidator()
    {
        RuleFor(expression => expression.GameId)
            .GreaterThan(0).WithMessage("Game Id cannot be less than 1");

        RuleFor(expression => expression.MarketHashName)
            .MinimumLength(3).WithMessage("The length of MarketHashName must be greater than 3 characters");
    }
}