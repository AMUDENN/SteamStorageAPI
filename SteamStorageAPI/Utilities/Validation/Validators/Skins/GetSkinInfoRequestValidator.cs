using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Skins;

public sealed class GetSkinInfoRequestValidator : AbstractValidator<GetSkinInfoRequest>
{
    public GetSkinInfoRequestValidator()
    {
        RuleFor(expression => expression.SkinId)
            .GreaterThan(0).WithMessage("Skin Id cannot be less than 1");
    }
}