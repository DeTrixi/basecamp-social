using BasecampSocial.Api.Models.Conversations;
using FluentValidation;

namespace BasecampSocial.Api.Validators;

public class CreateConversationRequestValidator : AbstractValidator<CreateConversationRequest>
{
    public CreateConversationRequestValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Conversation type is required.")
            .Must(t => t is "Direct" or "Group").WithMessage("Type must be 'Direct' or 'Group'.");

        RuleFor(x => x.MemberIds)
            .NotEmpty().WithMessage("At least one member is required.");

        RuleFor(x => x.MemberIds)
            .Must(m => m.Count == 1)
            .WithMessage("Direct conversations must have exactly one other member.")
            .When(x => x.Type == "Direct");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Group name is required for group conversations.")
            .When(x => x.Type == "Group");

        RuleFor(x => x.Name)
            .MaximumLength(100).WithMessage("Group name must not exceed 100 characters.")
            .When(x => x.Name is not null);
    }
}
