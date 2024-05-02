using FluentValidation;
using SteamStorageAPI.Controllers;

namespace SteamStorageAPI.Utilities.Validation.Validators.Actives;

public sealed class GetActiveInfoRequestValidator : AbstractValidator<ActivesController.GetActiveInfoRequest>
{
    public GetActiveInfoRequestValidator()
    {
        RuleFor(expression => expression.Id)
            .GreaterThan(0).WithMessage("Id актива не может быть меньше 1");
    }
}
