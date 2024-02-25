﻿using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.Archives;

public sealed class DeleteArchiveRequestValidator : AbstractValidator<ArchivesController.DeleteArchiveRequest>
{
    public DeleteArchiveRequestValidator()
    {
        RuleFor(expression => expression.Id)
            .GreaterThan(0).WithMessage("Id элемента архива не может быть меньше 1");
    }
}
