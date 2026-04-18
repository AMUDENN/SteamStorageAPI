using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Skins;

public sealed class DeleteMarkedSkinRequestValidator : AbstractValidator<DeleteMarkedSkinRequest>
{
    public DeleteMarkedSkinRequestValidator()
    {
        RuleFor(expression => expression.SkinId)
            .GreaterThan(0).WithMessage("Skin Id cannot be less than 1");
    }
}