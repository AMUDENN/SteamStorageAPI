using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Skins;

public sealed class SetMarkedSkinRequestValidator : AbstractValidator<SetMarkedSkinRequest>
{
    public SetMarkedSkinRequestValidator()
    {
        RuleFor(expression => expression.SkinId)
            .GreaterThan(0).WithMessage("Id предмета не может быть меньше 1");
    }
}