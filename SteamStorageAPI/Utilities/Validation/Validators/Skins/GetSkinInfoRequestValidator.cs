using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.Skins;

public sealed class GetSkinInfoRequestValidator : AbstractValidator<SkinsController.GetSkinInfoRequest>
{
    public GetSkinInfoRequestValidator()
    {
        RuleFor(expression => expression.SkinId)
            .GreaterThan(0).WithMessage("Id предмета не может быть меньше 1");
    }
}
