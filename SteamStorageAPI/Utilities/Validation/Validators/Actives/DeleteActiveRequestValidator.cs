using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Actives;

public sealed class DeleteActiveRequestValidator : AbstractValidator<DeleteActiveRequest>
{
    public DeleteActiveRequestValidator()
    {
        RuleFor(expression => expression.Id)
            .GreaterThan(0).WithMessage("Id актива не может быть меньше 1");
    }
}