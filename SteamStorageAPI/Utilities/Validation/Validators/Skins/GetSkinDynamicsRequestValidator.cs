using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Skins;

public sealed class GetSkinDynamicsRequestValidator : AbstractValidator<GetSkinDynamicsRequest>
{
    public GetSkinDynamicsRequestValidator()
    {
        RuleFor(expression => expression.SkinId)
            .GreaterThan(0).WithMessage("Skin Id cannot be less than 1");

        RuleFor(expression => expression.EndDate)
            .GreaterThan(expression => expression.StartDate)
            .WithMessage("The end date of the period must be greater than the start date of the period");
    }
}