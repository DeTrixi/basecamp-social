using BasecampSocial.Api.Models.Users;
using FluentValidation;

namespace BasecampSocial.Api.Validators;

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .MaximumLength(100).WithMessage("Display name must not exceed 100 characters.")
            .When(x => x.DisplayName is not null);

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(2048).WithMessage("Avatar URL must not exceed 2048 characters.")
            .When(x => x.AvatarUrl is not null);

        RuleFor(x => x.StatusMessage)
            .MaximumLength(200).WithMessage("Status message must not exceed 200 characters.")
            .When(x => x.StatusMessage is not null);
    }
}
