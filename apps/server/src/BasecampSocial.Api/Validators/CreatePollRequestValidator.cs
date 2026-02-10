using BasecampSocial.Api.Models.Polls;
using FluentValidation;

namespace BasecampSocial.Api.Validators;

public class CreatePollRequestValidator : AbstractValidator<CreatePollRequest>
{
    public CreatePollRequestValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty().WithMessage("Conversation ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.Options)
            .NotEmpty().WithMessage("At least one option is required.")
            .Must(o => o.Count <= 20).WithMessage("A poll can have at most 20 options.");

        RuleForEach(x => x.Options).ChildRules(option =>
        {
            option.RuleFor(o => o.Label)
                .MaximumLength(100).WithMessage("Option label must not exceed 100 characters.")
                .When(o => o.Label is not null);
        });
    }
}
