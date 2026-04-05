using FluentValidation;

namespace AtlasWeb.DTOs
{
    public class MusteriDtoValidator : AbstractValidator<MusteriDto>
    {
        public MusteriDtoValidator()
        {
            RuleFor(x => x.MusteriKodu)
                .NotEmpty()
                .MaximumLength(50);

            RuleFor(x => x.Unvan)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.VergiNo)
                .MaximumLength(11)
                .Matches(@"^\d{10,11}$")
                .When(x => !string.IsNullOrWhiteSpace(x.VergiNo));

            RuleFor(x => x.GsmNo)
                .MaximumLength(20)
                .Matches(@"^[\d\s\+\-\(\)]*$")
                .When(x => !string.IsNullOrWhiteSpace(x.GsmNo));

            RuleFor(x => x.EPosta)
                .NotEmpty()
                .EmailAddress();

            RuleFor(x => x.Il)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.Ilce)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.AdresDetay)
                .NotEmpty()
                .MaximumLength(250);
        }
    }
}
