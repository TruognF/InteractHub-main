using FluentValidation;
using InteractHub.API.DTOs;

namespace InteractHub.API.DTOs.Validators;

public class UpdateCommentValidator : AbstractValidator<UpdateCommentDto>
{
    public UpdateCommentValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content không được để trống")
            .MinimumLength(1).WithMessage("Content phải có ít nhất 1 ký tự")
            .MaximumLength(1000).WithMessage("Content không được vượt quá 1000 ký tự");
    }
}
