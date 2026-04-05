using FluentValidation;

namespace AtlasWeb.DTOs
{
    public class FaturaDetayDtoValidator : AbstractValidator<FaturaDetayDto>
    {
        public FaturaDetayDtoValidator()
        {
            RuleFor(x => x.StokId)
                .NotEmpty()
                .WithMessage("Her satir icin gecerli bir stok seciniz.");

            RuleFor(x => x.Miktar)
                .GreaterThan(0)
                .WithMessage("Miktar sifirdan buyuk olmalidir.");

            RuleFor(x => x.BirimFiyat)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Birim fiyat negatif olamaz.");
        }
    }

    public class FaturaDtoValidator : AbstractValidator<FaturaDto>
    {
        public FaturaDtoValidator()
        {
            RuleFor(x => x.CariKartId)
                .NotEmpty()
                .WithMessage("Fatura icin bir cari seciniz.");

            RuleFor(x => x.FaturaTarihi)
                .NotEmpty()
                .WithMessage("Fatura tarihi zorunludur.");

            RuleFor(x => x.Aciklama)
                .MaximumLength(300)
                .WithMessage("Aciklama 300 karakterden uzun olamaz.");

            RuleFor(x => x.Kalemler)
                .NotEmpty()
                .WithMessage("Fatura en az bir satir icermelidir.");

            RuleForEach(x => x.Kalemler)
                .SetValidator(new FaturaDetayDtoValidator());
        }
    }
}
