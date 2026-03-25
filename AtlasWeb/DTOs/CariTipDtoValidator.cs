using FluentValidation;

namespace AtlasWeb.DTOs
{
    public class CariTipDtoValidator : AbstractValidator<CariTipDto>
    {
        public CariTipDtoValidator()
        {
            RuleFor(x => x.Adi)
                .NotEmpty().WithMessage("Cari tip adı boş olamaz.")
                .MinimumLength(2).WithMessage("Cari tip adı en az 2 karakter olmalıdır.")
                .MaximumLength(50).WithMessage("Cari tip adı 50 karakteri geçemez.");

            RuleFor(x => x.Aciklama)
                .MaximumLength(200).WithMessage("Açıklama 200 karakteri geçemez.")
                .When(x => x.Aciklama != null);
        }
    }
}
