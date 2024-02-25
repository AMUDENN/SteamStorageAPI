using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.Currencies;

public sealed class DeleteCurrencyRequestValidator : AbstractValidator<CurrenciesController.DeleteCurrencyRequest>
{
    public DeleteCurrencyRequestValidator()
    {
        RuleFor(expression => expression.CurrencyId)
            .GreaterThan(0).WithMessage("Id валюты не может быть меньше 1");
    }
}
