using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Currencies;

public sealed class PostCurrencyRequestValidator : AbstractValidator<PostCurrencyRequest>
{
    public PostCurrencyRequestValidator()
    {
        RuleFor(expression => expression.SteamCurrencyId)
            .GreaterThan(0).WithMessage("Currency Id cannot be less than 1");

        RuleFor(expression => expression.Title)
            .Length(3, 100).WithMessage("The length of the currency name must be between 3 and 100 characters");

        RuleFor(expression => expression.Mark)
            .Length(1, 10).WithMessage("The length of the currency name must be between 1 and 10 characters");

        RuleFor(expression => expression.CultureInfo)
            .Length(1, 10).WithMessage("The length of CultureInfo must be between 1 and 10 characters");
    }
}