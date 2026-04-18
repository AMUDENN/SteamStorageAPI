using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Statistics;

public sealed class GetItemsCountByGameRequestValidator : AbstractValidator<GetItemsCountByGameRequest>
{
    public GetItemsCountByGameRequestValidator()
    {
        RuleFor(expression => expression.GameId)
            .GreaterThan(0).WithMessage("Game Id cannot be less than 1");
    }
}
