using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Currencies;

public sealed class SetCurrencyRequestValidator : AbstractValidator<SetCurrencyRequest>
{
    public SetCurrencyRequestValidator()
    {
        RuleFor(expression => expression.CurrencyId)
            .GreaterThan(0).WithMessage("Currency Id cannot be less than 1");
    }
}