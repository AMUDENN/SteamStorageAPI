using FluentValidation;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Utilities.Validation.Validators.ArchiveGroups;

public sealed class PostArchiveGroupRequestValidator : AbstractValidator<PostArchiveGroupRequest>
{
    public PostArchiveGroupRequestValidator()
    {
        RuleFor(expression => expression.Title)
            .Length(3, 100).WithMessage("The group name length must be between 3 and 100 characters");

        RuleFor(expression => expression.Description)
            .MaximumLength(300).WithMessage("The group description length must be between 0 and 300 characters");

        RuleFor(expression => expression.Colour)
            .Matches("^([A-Fa-f0-9]{8}|[A-Fa-f0-9]{6}|[A-Fa-f0-9]{4}|[A-Fa-f0-9]{3})$")
            .WithMessage(
                "The colour does not meet the requirements, examples of correct colour values: FA12AD29, FF1AFF, 2483, AD0");
    }
}