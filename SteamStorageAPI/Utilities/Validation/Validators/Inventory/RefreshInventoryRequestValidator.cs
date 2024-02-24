using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.Inventory;

public sealed class RefreshInventoryRequestValidator : AbstractValidator<InventoryController.RefreshInventoryRequest>
{
    public RefreshInventoryRequestValidator()
    {
        RuleFor(expression => expression.GameId)
            .GreaterThan(0).WithMessage("Id игры не может быть меньше 1")
            .LessThan(int.MaxValue).WithMessage($"Id игры не может быть больше {int.MaxValue}");
    }
}
