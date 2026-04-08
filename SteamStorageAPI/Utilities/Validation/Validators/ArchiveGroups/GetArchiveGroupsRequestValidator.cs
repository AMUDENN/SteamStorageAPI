using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.ArchiveGroups;

public sealed class GetArchiveGroupsRequestValidator : AbstractValidator<GetArchiveGroupsRequest>
{
    public GetArchiveGroupsRequestValidator()
    {
        RuleFor(expression => expression.OrderName)
            .IsInEnum().WithMessage("Порядок сортировки должен находиться в пределах от 0 до 4");
    }
}