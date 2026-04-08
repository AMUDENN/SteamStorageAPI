using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Inventory;

public sealed class GetInventoriesStatisticRequestValidator : AbstractValidator<GetInventoriesStatisticRequest>
{
    public GetInventoriesStatisticRequestValidator()
    {
        RuleFor(expression => expression.GameId)
            .GreaterThan(0).WithMessage("Id игры не может быть меньше 1");
    }
}