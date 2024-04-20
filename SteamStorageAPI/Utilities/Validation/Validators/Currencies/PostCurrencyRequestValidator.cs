using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.Currencies;

public sealed class PostCurrencyRequestValidator : AbstractValidator<CurrenciesController.PostCurrencyRequest>
{
    public PostCurrencyRequestValidator()
    {
        RuleFor(expression => expression.SteamCurrencyId)
            .GreaterThan(0).WithMessage("Id валюты не может быть меньше 1");
        
        RuleFor(expression => expression.Title)
            .Length(3, 100).WithMessage("Длина названия валюты должна быть от 3 до 100 символов");
        
        RuleFor(expression => expression.Mark)
            .Length(1, 10).WithMessage("Длина названия валюты должна быть от 1 до 10 символов");
        
        RuleFor(expression => expression.CultureInfo)
            .Length(1, 10).WithMessage("Длина CultureInfo должна быть от 1 до 10 символов");
    }
}
