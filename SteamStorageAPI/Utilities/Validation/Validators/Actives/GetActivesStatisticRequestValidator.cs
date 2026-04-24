using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Actives;

public sealed class GetActivesStatisticRequestValidator : AbstractValidator<GetActivesStatisticRequest>
{
    public GetActivesStatisticRequestValidator()
    {
        RuleFor(expression => expression.GroupId)
            .GreaterThan(0).WithMessage("Group Id cannot be less than 1");

        RuleFor(expression => expression.GameId)
            .GreaterThan(0).WithMessage("Game Id cannot be less than 1");
    }
}