using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Actives;

public sealed class GetActiveInfoRequestValidator : AbstractValidator<GetActiveInfoRequest>
{
    public GetActiveInfoRequestValidator()
    {
        RuleFor(expression => expression.Id)
            .GreaterThan(0).WithMessage("Active Id cannot be less than 1");
    }
}