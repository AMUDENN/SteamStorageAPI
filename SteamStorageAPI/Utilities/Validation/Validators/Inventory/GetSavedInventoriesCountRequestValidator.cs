using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Inventory;

public sealed class GetSavedInventoriesCountRequestValidator : AbstractValidator<GetSavedInventoriesCountRequest>
{
    public GetSavedInventoriesCountRequestValidator()
    {
        RuleFor(expression => expression.GameId)
            .GreaterThan(0).WithMessage("Game Id cannot be less than 1");
    }
}