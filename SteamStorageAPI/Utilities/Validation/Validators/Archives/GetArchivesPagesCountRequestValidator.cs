using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.Archives;

public sealed class GetArchivesPagesCountRequestValidator : AbstractValidator<GetArchivesPagesCountRequest>
{
    public GetArchivesPagesCountRequestValidator()
    {
        RuleFor(expression => expression.GroupId)
            .GreaterThan(0).WithMessage("Id группы не может быть меньше 1");

        RuleFor(expression => expression.GameId)
            .GreaterThan(0).WithMessage("Id игры не может быть меньше 1");

        RuleFor(expression => expression.PageSize)
            .InclusiveBetween(1, 200).WithMessage("Размер страницы должен находиться в интервале от 1 до 200");
    }
}