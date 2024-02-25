using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.Skins;

public sealed class GetSkinDynamicsRequestValidator : AbstractValidator<SkinsController.GetSkinDynamicsRequest>
{
    public GetSkinDynamicsRequestValidator()
    {
        RuleFor(expression => expression.SkinId)
            .GreaterThan(0).WithMessage("Id предмета не может быть меньше 1");
        
        RuleFor(expression => expression.EndDate)
            .GreaterThan(expression => expression.StartDate)
            .WithMessage("Дата конца периода должна быть больше даты начала периода");
    }
}
