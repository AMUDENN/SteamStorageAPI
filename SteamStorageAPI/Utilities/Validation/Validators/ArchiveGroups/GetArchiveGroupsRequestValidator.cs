using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.ArchiveGroups;

public sealed class GetArchiveGroupsRequestValidator : AbstractValidator<GetArchiveGroupsRequest>
{
    public GetArchiveGroupsRequestValidator()
    {
        RuleFor(expression => expression.OrderName)
            .IsInEnum().WithMessage("Sort order must be in the range from 0 to 4");
    }
}