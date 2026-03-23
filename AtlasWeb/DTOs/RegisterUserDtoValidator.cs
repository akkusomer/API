using FluentValidation;

namespace AtlasWeb.DTOs
{
    public class RegisterUserDtoValidator : AbstractValidator<RegisterUserDto>
    {
        public RegisterUserDtoValidator()
        {
            RuleFor(x => x.MusteriId).NotEmpty().WithMessage("Müşteri/Şirket ID'si belirtilmelidir.");
            RuleFor(x => x.Ad).NotEmpty().WithMessage("Ad boş olamaz.").MaximumLength(50).WithMessage("Ad 50 karakterden uzun olamaz.");
            RuleFor(x => x.Soyad).NotEmpty().WithMessage("Soyad boş olamaz.").MaximumLength(50).WithMessage("Soyad 50 karakterden uzun olamaz.");
            RuleFor(x => x.EPosta).NotEmpty().WithMessage("E-posta boş olamaz.").EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.");
            RuleFor(x => x.Sifre).NotEmpty().WithMessage("Şifre boş olamaz.").MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır.");
        }
    }
}
