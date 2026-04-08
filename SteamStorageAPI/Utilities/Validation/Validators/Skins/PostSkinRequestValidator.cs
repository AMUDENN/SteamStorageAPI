using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Skins;

public sealed class PostSkinRequestValidator : AbstractValidator<PostSkinRequest>
{
    public PostSkinRequestValidator()
    {
        RuleFor(expression => expression.GameId)
            .GreaterThan(0).WithMessage("Id игры не может быть меньше 1");

        RuleFor(expression => expression.MarketHashName)
            .MinimumLength(3).WithMessage("Длина MarketHashName должна быть больше 3 символов");
    }
}