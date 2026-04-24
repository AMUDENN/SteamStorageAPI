using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Archives;

public sealed class GetArchiveInfoRequestValidator : AbstractValidator<GetArchiveInfoRequest>
{
    public GetArchiveInfoRequestValidator()
    {
        RuleFor(expression => expression.Id)
            .GreaterThan(0).WithMessage("Archive item Id cannot be less than 1");
    }
}