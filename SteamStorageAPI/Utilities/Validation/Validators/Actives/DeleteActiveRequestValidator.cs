using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.Actives;

public sealed class DeleteActiveRequestValidator : AbstractValidator<ActivesController.DeleteActiveRequest>
{
    public DeleteActiveRequestValidator()
    {
        RuleFor(expression => expression.Id)
            .GreaterThan(0).WithMessage("Id актива не может быть меньше 1")
            .LessThan(int.MaxValue).WithMessage($"Id актива не может быть больше {int.MaxValue}");
    }
}
