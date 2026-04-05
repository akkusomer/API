using FluentValidation;

namespace AtlasWeb.DTOs
{
    public class RegisterUserDtoValidator : AbstractValidator<RegisterUserDto>
    {
        public RegisterUserDtoValidator()
        {
            RuleFor(x => x.MusteriId)
                .NotEmpty()
                .WithMessage("Musteri ID belirtilmelidir.");

            RuleFor(x => x.Ad)
                .NotEmpty()
                .MaximumLength(50);

            RuleFor(x => x.Soyad)
                .NotEmpty()
                .MaximumLength(50);

            RuleFor(x => x.EPosta)
                .NotEmpty()
                .EmailAddress();

            RuleFor(x => x.Telefon)
                .MaximumLength(20)
                .When(x => !string.IsNullOrWhiteSpace(x.Telefon));

            RuleFor(x => x.Sifre)
                .NotEmpty()
                .MinimumLength(8)
                .MaximumLength(128)
                .Matches("[A-Z]").WithMessage("Sifre en az bir buyuk harf icermelidir.")
                .Matches("[a-z]").WithMessage("Sifre en az bir kucuk harf icermelidir.")
                .Matches("[0-9]").WithMessage("Sifre en az bir rakam icermelidir.")
                .Matches("[^a-zA-Z0-9]").WithMessage("Sifre en az bir ozel karakter icermelidir.");
        }
    }
}
