﻿using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.Actives;

public sealed class PutActiveRequestValidator : AbstractValidator<ActivesController.PutActiveRequest>
{
    public PutActiveRequestValidator()
    {
        RuleFor(expression => expression.Id)
            .GreaterThan(0).WithMessage("Id актива не может быть меньше 1");
        
        RuleFor(expression => expression.GroupId)
            .GreaterThan(0).WithMessage("Id группы не может быть меньше 1");

        RuleFor(expression => expression.Count)
            .GreaterThan(0).WithMessage("Количество предметов не может быть меньше 1");

        RuleFor(expression => expression.BuyPrice)
            .GreaterThanOrEqualTo((decimal)0.01).WithMessage("Стоимость покупки не может быть меньше 0.01")
            .LessThan(1000000000000).WithMessage("Стоимость покупки не может быть больше 999999999999");

        RuleFor(expression => expression.GoalPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Финансовая цель не может быть меньше 0")
            .LessThan(1000000000000).WithMessage("Финансовая цель не может быть больше 999999999999");

        RuleFor(expression => expression.SkinId)
            .GreaterThan(0).WithMessage("Id предмета не может быть меньше 1");

        RuleFor(expression => expression.Description)
            .MaximumLength(300).WithMessage("Длина описания предмета должна быть от 0 до 300 символов");
    }
}
