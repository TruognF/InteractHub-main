using FluentValidation;
using InteractHub.API.DTOs;

namespace InteractHub.API.DTOs.Validators;

public class UpdateHashtagValidator : AbstractValidator<UpdateHashtagDto>
{
    public UpdateHashtagValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name không được để trống")
            .MinimumLength(1).WithMessage("Name phải có ít nhất 1 ký tự")
            .MaximumLength(100).WithMessage("Name không được vượt quá 100 ký tự")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Name chỉ được chứa chữ, số và dấu gạch dưới");
    }
}
