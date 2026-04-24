using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Archives;

public sealed class DeleteArchiveRequestValidator : AbstractValidator<DeleteArchiveRequest>
{
    public DeleteArchiveRequestValidator()
    {
        RuleFor(expression => expression.Id)
            .GreaterThan(0).WithMessage("Archive item Id cannot be less than 1");
    }
}