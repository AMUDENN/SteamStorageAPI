using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.Currencies;

public sealed class SetCurrencyRequestValidator : AbstractValidator<CurrenciesController.SetCurrencyRequest>
{
    public SetCurrencyRequestValidator()
    {
        RuleFor(expression => expression.CurrencyId)
            .GreaterThan(0).WithMessage("Id валюты не может быть меньше 1")
            .LessThan(int.MaxValue).WithMessage($"Id валюты не может быть больше {int.MaxValue}");
    }
}
