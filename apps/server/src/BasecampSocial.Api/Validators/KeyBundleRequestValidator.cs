using BasecampSocial.Api.Models.Keys;
using FluentValidation;

namespace BasecampSocial.Api.Validators;

public class KeyBundleRequestValidator : AbstractValidator<KeyBundleRequest>
{
    public KeyBundleRequestValidator()
    {
        RuleFor(x => x.IdentityKey)
            .NotEmpty().WithMessage("Identity key is required.");

        RuleFor(x => x.SignedPreKey)
            .NotEmpty().WithMessage("Signed pre-key is required.");

        RuleFor(x => x.SignedPreKeySignature)
            .NotEmpty().WithMessage("Signed pre-key signature is required.");

        RuleFor(x => x.OneTimePreKeys)
            .NotEmpty().WithMessage("At least one one-time pre-key is required.");
    }
}
