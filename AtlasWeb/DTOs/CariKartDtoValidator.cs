using FluentValidation;

namespace AtlasWeb.DTOs
{
    public class CariKartDtoValidator : AbstractValidator<CariKartDto>
    {
        public CariKartDtoValidator()
        {
            RuleFor(x => x.CariTipId)
                .NotEmpty().WithMessage("Cari tip secimi zorunludur.");

            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.Unvan) || !string.IsNullOrWhiteSpace(x.AdiSoyadi))
                .WithName("Kimlik")
                .WithMessage("Unvan veya Ad Soyad alanlarindan en az biri girilmelidir.");

            RuleFor(x => x.Unvan)
                .MaximumLength(150).WithMessage("Unvan 150 karakteri gecemez.")
                .When(x => x.Unvan != null);

            RuleFor(x => x.AdiSoyadi)
                .MaximumLength(100).WithMessage("Ad Soyad 100 karakteri gecemez.")
                .When(x => x.AdiSoyadi != null);

            RuleFor(x => x.FaturaTipi)
                .IsInEnum().WithMessage("Gecersiz fatura tipi.");

            RuleFor(x => x.GrupKodu)
                .MaximumLength(20).WithMessage("Grup kodu 20 karakteri gecemez.")
                .When(x => x.GrupKodu != null);

            RuleFor(x => x.OzelKodu)
                .MaximumLength(20).WithMessage("Ozel kod 20 karakteri gecemez.")
                .When(x => x.OzelKodu != null);

            RuleFor(x => x.Telefon)
                .MaximumLength(20).WithMessage("Telefon 20 karakteri gecemez.")
                .Matches(@"^[\d\s\+\-\(\)]*$").WithMessage("Gecersiz telefon formati.")
                .When(x => !string.IsNullOrWhiteSpace(x.Telefon));

            RuleFor(x => x.Telefon2)
                .MaximumLength(20).WithMessage("Telefon2 20 karakteri gecemez.")
                .Matches(@"^[\d\s\+\-\(\)]*$").WithMessage("Gecersiz telefon formati.")
                .When(x => !string.IsNullOrWhiteSpace(x.Telefon2));

            RuleFor(x => x.Gsm)
                .MaximumLength(20).WithMessage("GSM 20 karakteri gecemez.")
                .Matches(@"^[\d\s\+\-\(\)]*$").WithMessage("Gecersiz GSM formati.")
                .When(x => !string.IsNullOrWhiteSpace(x.Gsm));

            RuleFor(x => x.Adres)
                .MaximumLength(250).WithMessage("Adres 250 karakteri gecemez.")
                .When(x => x.Adres != null);

            RuleFor(x => x.Il)
                .MaximumLength(100).WithMessage("Il 100 karakteri gecemez.")
                .When(x => x.Il != null);

            RuleFor(x => x.VergiDairesi)
                .MaximumLength(50).WithMessage("Vergi dairesi 50 karakteri gecemez.")
                .When(x => x.VergiDairesi != null);

            RuleFor(x => x.VTCK_No)
                .MaximumLength(11).WithMessage("VKN/TCKN en fazla 11 karakter olabilir.")
                .Matches(@"^\d{10,11}$").WithMessage("VKN 10, TCKN 11 haneli olmalidir.")
                .When(x => !string.IsNullOrWhiteSpace(x.VTCK_No));
        }
    }
}
