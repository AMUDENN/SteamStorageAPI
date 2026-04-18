using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Currencies;

public sealed class GetCurrencyRequestValidator : AbstractValidator<GetCurrencyRequest>
{
    public GetCurrencyRequestValidator()
    {
        RuleFor(expression => expression.Id)
            .GreaterThan(0).WithMessage("Currency Id cannot be less than 1");
    }
}