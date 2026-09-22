using FluentValidation;
using InteractHub.API.DTOs;

namespace InteractHub.API.DTOs.Validators;

public class CreateUserValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("UserName không được để trống")
            .MinimumLength(3).WithMessage("UserName phải có ít nhất 3 ký tự")
            .MaximumLength(50).WithMessage("UserName không được vượt quá 50 ký tự")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("UserName chỉ được chứa chữ, số và dấu gạch dưới");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống")
            .EmailAddress().WithMessage("Email không hợp lệ");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password không được để trống")
            .MinimumLength(6).WithMessage("Password phải có ít nhất 6 ký tự")
            .MaximumLength(100).WithMessage("Password không được vượt quá 100 ký tự");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("FullName không được để trống")
            .MinimumLength(2).WithMessage("FullName phải có ít nhất 2 ký tự")
            .MaximumLength(100).WithMessage("FullName không được vượt quá 100 ký tự");
    }
}
