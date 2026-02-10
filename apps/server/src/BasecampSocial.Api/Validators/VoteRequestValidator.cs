using BasecampSocial.Api.Models.Polls;
using FluentValidation;

namespace BasecampSocial.Api.Validators;

public class VoteRequestValidator : AbstractValidator<VoteRequest>
{
    public VoteRequestValidator()
    {
        RuleFor(x => x.PollOptionId)
            .NotEmpty().WithMessage("Poll option ID is required.");

        RuleFor(x => x.Response)
            .NotEmpty().WithMessage("Response is required.")
            .Must(r => r is "Yes" or "Maybe" or "No")
            .WithMessage("Response must be 'Yes', 'Maybe', or 'No'.");
    }
}
