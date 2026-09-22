using FluentValidation;
using InteractHub.API.DTOs;

namespace InteractHub.API.DTOs.Validators;

public class CreateCommentValidator : AbstractValidator<CreateCommentDto>
{
    public CreateCommentValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content không được để trống")
            .MinimumLength(1).WithMessage("Content phải có ít nhất 1 ký tự")
            .MaximumLength(1000).WithMessage("Content không được vượt quá 1000 ký tự");

        RuleFor(x => x.PostId)
            .GreaterThan(0).WithMessage("PostId phải lớn hơn 0");
    }
}
