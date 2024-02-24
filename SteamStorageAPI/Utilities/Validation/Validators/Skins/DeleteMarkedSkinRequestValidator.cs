using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.Skins;

public sealed class DeleteMarkedSkinRequestValidator : AbstractValidator<SkinsController.DeleteMarkedSkinRequest>
{
    public DeleteMarkedSkinRequestValidator()
    {
        RuleFor(expression => expression.SkinId)
            .GreaterThan(0).WithMessage("Id предмета не может быть меньше 1")
            .LessThan(int.MaxValue).WithMessage($"Id предмета не может быть больше {int.MaxValue}");
    }
}
