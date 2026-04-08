using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Skins;

public sealed class GetSteamSkinsCountRequestValidator : AbstractValidator<GetSteamSkinsCountRequest>
{
    public GetSteamSkinsCountRequestValidator()
    {
        RuleFor(expression => expression.GameId)
            .GreaterThan(0).WithMessage("Id игры не может быть меньше 1");
    }
}