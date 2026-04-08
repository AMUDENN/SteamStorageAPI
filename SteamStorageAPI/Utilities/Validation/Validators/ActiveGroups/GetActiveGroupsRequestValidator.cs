using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.ActiveGroups;

public sealed class GetActiveGroupsRequestValidator : AbstractValidator<GetActiveGroupsRequest>
{
    public GetActiveGroupsRequestValidator()
    {
        RuleFor(expression => expression.OrderName)
            .IsInEnum().WithMessage("Порядок сортировки должен находиться в пределах от 0 до 4");
    }
}