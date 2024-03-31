using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.Inventory;

public sealed class GetInventoriesStatisticRequestValidator : AbstractValidator<InventoryController.GetInventoriesStatisticRequest>
{
    public GetInventoriesStatisticRequestValidator()
    {
        RuleFor(expression => expression.GameId)
            .GreaterThan(0).WithMessage("Id игры не может быть меньше 1");
    }
}
