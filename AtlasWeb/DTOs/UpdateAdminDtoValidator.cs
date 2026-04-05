using FluentValidation;

namespace AtlasWeb.DTOs
{
    public class UpdateAdminDtoValidator : AbstractValidator<UpdateAdminDto>
    {
        public UpdateAdminDtoValidator()
        {
            RuleFor(x => x.Ad)
                .NotEmpty()
                .MaximumLength(50);

            RuleFor(x => x.Soyad)
                .NotEmpty()
                .MaximumLength(50);

            RuleFor(x => x.Telefon)
                .MaximumLength(20)
                .When(x => !string.IsNullOrWhiteSpace(x.Telefon));
        }
    }
}
