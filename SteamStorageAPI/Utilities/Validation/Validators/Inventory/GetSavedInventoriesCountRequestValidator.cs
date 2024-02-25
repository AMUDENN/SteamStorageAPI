using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.Inventory;

public sealed class GetSavedInventoriesCountRequestValidator : AbstractValidator<InventoryController.GetSavedInventoriesCountRequest>
{
    public GetSavedInventoriesCountRequestValidator()
    {
        RuleFor(expression => expression.GameId)
            .GreaterThan(0).WithMessage("Id игры не может быть меньше 1");
    }
}
