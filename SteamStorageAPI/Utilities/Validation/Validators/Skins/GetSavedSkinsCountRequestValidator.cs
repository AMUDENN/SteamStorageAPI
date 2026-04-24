using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Skins;

public sealed class GetSavedSkinsCountRequestValidator : AbstractValidator<GetSavedSkinsCountRequest>
{
    public GetSavedSkinsCountRequestValidator()
    {
        RuleFor(expression => expression.GameId)
            .GreaterThan(0).WithMessage("Game Id cannot be less than 1");
    }
}