using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Actives;

public sealed class PutActiveRequestValidator : AbstractValidator<PutActiveRequest>
{
    public PutActiveRequestValidator()
    {
        RuleFor(expression => expression.Id)
            .GreaterThan(0).WithMessage("Active Id cannot be less than 1");

        RuleFor(expression => expression.GroupId)
            .GreaterThan(0).WithMessage("Group Id cannot be less than 1");

        RuleFor(expression => expression.Count)
            .GreaterThan(0).WithMessage("Item count cannot be less than 1");

        RuleFor(expression => expression.BuyPrice)
            .GreaterThanOrEqualTo((decimal)0.01).WithMessage("Buy price cannot be less than 0.01")
            .LessThan(1000000000000).WithMessage("Buy price cannot be greater than 999999999999");

        RuleFor(expression => expression.GoalPrice)
            .GreaterThanOrEqualTo(0).WithMessage("The financial goal cannot be less than 0")
            .LessThan(1000000000000).WithMessage("The financial goal cannot be greater than 999999999999");

        RuleFor(expression => expression.SkinId)
            .GreaterThan(0).WithMessage("Skin Id cannot be less than 1");

        RuleFor(expression => expression.Description)
            .MaximumLength(300).WithMessage("The item description length must be between 0 and 300 characters");
    }
}