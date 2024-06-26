﻿using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.Skins;

public sealed class GetSkinPagesCountRequestValidator : AbstractValidator<SkinsController.GetSkinPagesCountRequest>
{
    public GetSkinPagesCountRequestValidator()
    {
        RuleFor(expression => expression.GameId)
            .GreaterThan(0).WithMessage("Id игры не может быть меньше 1");

        RuleFor(expression => expression.PageSize)
            .InclusiveBetween(1, 200).WithMessage("Размер страницы должен находиться в интервале от 1 до 200");
    }
}
