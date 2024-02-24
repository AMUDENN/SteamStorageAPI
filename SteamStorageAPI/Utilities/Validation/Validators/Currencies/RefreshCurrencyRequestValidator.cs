using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.Currencies;

public sealed class RefreshCurrencyRequestValidator : AbstractValidator<CurrenciesController.RefreshCurrencyRequest>
{
    public RefreshCurrencyRequestValidator()
    {
        RuleFor(expression => expression.MarketHashName)
            .MinimumLength(3).WithMessage("Длина MarketHashName должна быть больше 3 символов");
    }
}
