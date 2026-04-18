using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Currencies;

public sealed class GetCurrencyDynamicsRequestValidator : AbstractValidator<GetCurrencyDynamicsRequest>
{
    public GetCurrencyDynamicsRequestValidator()
    {
        RuleFor(expression => expression.CurrencyId)
            .GreaterThan(0).WithMessage("Currency Id cannot be less than 1");
    }
}
