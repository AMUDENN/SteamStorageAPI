using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.ActiveGroups;

public sealed class GetActiveGroupsRequestValidator : AbstractValidator<GetActiveGroupsRequest>
{
    public GetActiveGroupsRequestValidator()
    {
        RuleFor(expression => expression.OrderName)
            .IsInEnum().WithMessage("Sort order must be in the range from 0 to 4");
    }
}