using FluentValidation;

namespace AtlasWeb.DTOs
{
    public class StokDtoValidator : AbstractValidator<StokDto>
    {
        public StokDtoValidator()
        {
            RuleFor(x => x.StokAdi).NotEmpty().WithMessage("Stok adı zorunludur.").MaximumLength(200).WithMessage("Stok adı 200 karakterden uzun olamaz.");
            RuleFor(x => x.YedekAdi).MaximumLength(200).WithMessage("Yedek ad 200 karakterden uzun olamaz.");
            RuleFor(x => x.BirimId).NotEmpty().WithMessage("Stok için bir geçerli ölçü birimi seçiniz.");
        }
    }
}
