using FluentValidation;

namespace AtlasWeb.DTOs
{
    public class LoginDtoValidator : AbstractValidator<LoginDto>
    {
        public LoginDtoValidator()
        {
            RuleFor(x => x.EPosta).NotEmpty().WithMessage("E-posta boş olamaz.").EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");
            RuleFor(x => x.Sifre).NotEmpty().WithMessage("Şifre boş olamaz.");
        }
    }
}
