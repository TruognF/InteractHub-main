using FluentValidation;
using InteractHub.API.DTOs;

namespace InteractHub.API.DTOs.Validators;

public class CreateNotificationValidator : AbstractValidator<CreateNotificationDto>
{
    public CreateNotificationValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content không được để trống")
            .MinimumLength(1).WithMessage("Content phải có ít nhất 1 ký tự")
            .MaximumLength(500).WithMessage("Content không được vượt quá 500 ký tự");
    }
}
