using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Inventory;

public sealed class RefreshInventoryRequestValidator : AbstractValidator<RefreshInventoryRequest>
{
    public RefreshInventoryRequestValidator()
    {
        RuleFor(expression => expression.GameId)
            .GreaterThan(0).WithMessage("Game Id cannot be less than 1");
    }
}