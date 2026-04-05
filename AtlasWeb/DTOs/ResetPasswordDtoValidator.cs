using FluentValidation;

namespace AtlasWeb.DTOs
{
    public class ResetPasswordDtoValidator : AbstractValidator<ResetPasswordDto>
    {
        public ResetPasswordDtoValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty()
                .WithMessage("Token gereklidir.");

            RuleFor(x => x.YeniSifre)
                .NotEmpty()
                .MinimumLength(8)
                .MaximumLength(128)
                .Matches("[A-Z]").WithMessage("Sifre en az bir buyuk harf icermelidir.")
                .Matches("[a-z]").WithMessage("Sifre en az bir kucuk harf icermelidir.")
                .Matches("[0-9]").WithMessage("Sifre en az bir rakam icermelidir.")
                .Matches("[^a-zA-Z0-9]").WithMessage("Sifre en az bir ozel karakter icermelidir.");

            RuleFor(x => x.YeniSifreTekrar)
                .Equal(x => x.YeniSifre)
                .WithMessage("Sifre tekrari eslesmiyor.");
        }
    }
}
