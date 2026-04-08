using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Currencies;

public sealed class DeleteCurrencyRequestValidator : AbstractValidator<DeleteCurrencyRequest>
{
    public DeleteCurrencyRequestValidator()
    {
        RuleFor(expression => expression.CurrencyId)
            .GreaterThan(0).WithMessage("Id валюты не может быть меньше 1");
    }
}