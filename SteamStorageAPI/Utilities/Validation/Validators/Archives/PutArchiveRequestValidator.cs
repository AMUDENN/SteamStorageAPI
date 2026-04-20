using FluentValidation;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Utilities.Validation;
using SteamStorageAPI.Utilities.Validation.Tools;

namespace SteamStorageAPI.Utilities.Validation.Validators.Archives;

public sealed class PutArchiveRequestValidator : AbstractValidator<PutArchiveRequest>
{
    public PutArchiveRequestValidator()
    {
        RuleFor(expression => expression.Id)
            .GreaterThan(0).WithMessage("Archive item Id cannot be less than 1");

        RuleFor(expression => expression.GroupId)
            .GreaterThan(0).WithMessage("Group Id cannot be less than 1");

        RuleFor(expression => expression.Count)
            .GreaterThan(0).WithMessage("Item count cannot be less than 1");

        RuleFor(expression => expression.BuyPrice)
            .GreaterThanOrEqualTo((decimal)0.01).WithMessage("Buy price cannot be less than 0.01")
            .LessThan(ValidationConstants.MaxPrice).WithMessage("Buy price cannot be greater than 999999999999");

        RuleFor(expression => expression.SoldPrice)
            .GreaterThanOrEqualTo((decimal)0.01).WithMessage("Sold price cannot be less than 0.01")
            .LessThan(ValidationConstants.MaxPrice).WithMessage("Sold price cannot be greater than 999999999999");

        RuleFor(expression => expression.SkinId)
            .GreaterThan(0).WithMessage("Skin Id cannot be less than 1");

        RuleFor(expression => expression.Description)
            .MaximumLength(300).WithMessage("The item description length must be between 0 and 300 characters");

        RuleFor(expression => expression.SoldDate)
            .GreaterThanOrEqualTo(expression => expression.BuyDate)
            .WithMessage("The sold date must be greater than or equal to the buy date");
    }
}