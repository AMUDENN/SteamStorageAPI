using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Currencies;

public sealed class GetCurrencyRequestValidator : AbstractValidator<GetCurrencyRequest>
{
    public GetCurrencyRequestValidator()
    {
        RuleFor(expression => expression.Id)
            .GreaterThan(0).WithMessage("Id валюты не может быть меньше 1");
    }
}