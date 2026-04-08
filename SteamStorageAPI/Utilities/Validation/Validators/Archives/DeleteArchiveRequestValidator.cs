using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Archives;

public sealed class DeleteArchiveRequestValidator : AbstractValidator<DeleteArchiveRequest>
{
    public DeleteArchiveRequestValidator()
    {
        RuleFor(expression => expression.Id)
            .GreaterThan(0).WithMessage("Id элемента архива не может быть меньше 1");
    }
}