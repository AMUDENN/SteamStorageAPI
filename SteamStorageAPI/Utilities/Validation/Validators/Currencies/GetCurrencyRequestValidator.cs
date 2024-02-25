using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.Currencies;

public sealed class GetCurrencyRequestValidator : AbstractValidator<CurrenciesController.GetCurrencyRequest>
{
    public GetCurrencyRequestValidator()
    {
        RuleFor(expression => expression.Id)
            .GreaterThan(0).WithMessage("Id валюты не может быть меньше 1");
    }
}
