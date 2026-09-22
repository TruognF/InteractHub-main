using FluentValidation;
using InteractHub.API.DTOs;

namespace InteractHub.API.DTOs.Validators;

public class CreatePostReportValidator : AbstractValidator<CreatePostReportDto>
{
    public CreatePostReportValidator()
    {
        RuleFor(x => x.Reason)
            .IsInEnum().WithMessage("Reason không hợp lệ");

        RuleFor(x => x.Detail)
            .MaximumLength(500).WithMessage("Chi tiết báo cáo không được vượt quá 500 ký tự");

        RuleFor(x => x.PostId)
            .GreaterThan(0).WithMessage("PostId phải lớn hơn 0");
    }
}
