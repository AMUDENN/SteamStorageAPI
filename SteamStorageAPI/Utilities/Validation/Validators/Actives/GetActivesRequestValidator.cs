﻿using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.Actives;

public sealed class GetActivesRequestValidator : AbstractValidator<ActivesController.GetActivesRequest>
{
    public GetActivesRequestValidator()
    {
        RuleFor(expression => expression.GroupId)
            .GreaterThan(0).WithMessage("Id группы не может быть меньше 1");

        RuleFor(expression => expression.GameId)
            .GreaterThan(0).WithMessage("Id игры не может быть меньше 1");

        RuleFor(expression => expression.OrderName)
            .IsInEnum().WithMessage("Порядок сортировки должен находиться в пределах от 0 до 5");

        RuleFor(expression => expression.PageNumber)
            .GreaterThan(0).WithMessage("Номер страницы не может быть меньше 1");

        RuleFor(expression => expression.PageSize)
            .InclusiveBetween(1, 200).WithMessage("Размер страницы должен находиться в интервале от 1 до 200");
    }
}
