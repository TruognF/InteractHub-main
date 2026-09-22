using FluentValidation;
using InteractHub.API.DTOs;

namespace InteractHub.API.DTOs.Validators;

public class UpdateUserValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("FullName không được để trống")
            .MinimumLength(2).WithMessage("FullName phải có ít nhất 2 ký tự")
            .MaximumLength(100).WithMessage("FullName không được vượt quá 100 ký tự");

        RuleFor(x => x.ProfilePictureUrl)
            .Must(BeValidUrl).WithMessage("ProfilePictureUrl phải là URL hợp lệ")
            .When(x => !string.IsNullOrEmpty(x.ProfilePictureUrl));

        RuleFor(x => x.Bio)
            .MaximumLength(500).WithMessage("Bio không được vượt quá 500 ký tự");
    }

    private bool BeValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) 
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
