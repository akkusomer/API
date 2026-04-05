using FluentValidation;

namespace AtlasWeb.DTOs
{
    public class ForgotPasswordRequestDtoValidator : AbstractValidator<ForgotPasswordRequestDto>
    {
        public ForgotPasswordRequestDtoValidator()
        {
            RuleFor(x => x.EPosta)
                .NotEmpty().WithMessage("E-posta bos olamaz.")
                .EmailAddress().WithMessage("Gecerli bir e-posta adresi giriniz.");
        }
    }
}
