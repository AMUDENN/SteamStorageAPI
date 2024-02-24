using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.Skins;

public sealed class PostSkinRequestValidator : AbstractValidator<SkinsController.PostSkinRequest>
{
    public PostSkinRequestValidator()
    {
        RuleFor(expression => expression.GameId)
            .GreaterThan(0).WithMessage("Id игры не может быть меньше 1")
            .LessThan(int.MaxValue).WithMessage($"Id игры не может быть больше {int.MaxValue}");
        
        RuleFor(expression => expression.MarketHashName)
            .MinimumLength(3).WithMessage("Длина MarketHashName должна быть больше 3 символов");
    }
}
