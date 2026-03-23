using FluentValidation;

namespace AtlasWeb.DTOs
{
    public class BirimDtoValidator : AbstractValidator<BirimDto>
    {
        public BirimDtoValidator()
        {
            RuleFor(x => x.Ad).NotEmpty().WithMessage("Birim adı boş olamaz.").MaximumLength(50).WithMessage("En fazla 50 karakter olabilir.");
            RuleFor(x => x.Sembol).NotEmpty().WithMessage("Sembol boş olamaz.").MaximumLength(10).WithMessage("En fazla 10 karakter olabilir.");
        }
    }
}
