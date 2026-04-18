using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Skins;

public sealed class GetSkinsRequestValidator : AbstractValidator<GetSkinsRequest>
{
    public GetSkinsRequestValidator()
    {
        RuleFor(expression => expression.GameId)
            .GreaterThan(0).WithMessage("Game Id cannot be less than 1");

        RuleFor(expression => expression.OrderName)
            .IsInEnum().WithMessage("Sort order must be in the range from 0 to 3");

        RuleFor(expression => expression.PageNumber)
            .GreaterThan(0).WithMessage("Page number cannot be less than 1");

        RuleFor(expression => expression.PageSize)
            .InclusiveBetween(1, 200).WithMessage("Page size must be in the range from 1 to 200");
    }
}