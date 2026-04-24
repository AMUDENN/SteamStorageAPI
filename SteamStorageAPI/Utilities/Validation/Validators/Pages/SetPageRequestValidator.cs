using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Pages;

public sealed class SetPageRequestValidator : AbstractValidator<SetPageRequest>
{
    public SetPageRequestValidator()
    {
        RuleFor(expression => expression.PageId)
            .GreaterThan(0).WithMessage("Page Id cannot be less than 1");
    }
}