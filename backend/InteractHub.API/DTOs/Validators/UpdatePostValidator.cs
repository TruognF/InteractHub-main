using FluentValidation;
using InteractHub.API.DTOs;

namespace InteractHub.API.DTOs.Validators;

public class UpdatePostValidator : AbstractValidator<UpdatePostDto>
{
    public UpdatePostValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content không được để trống")
            .MinimumLength(1).WithMessage("Content phải có ít nhất 1 ký tự")
            .MaximumLength(5000).WithMessage("Content không được vượt quá 5000 ký tự");

        RuleFor(x => x.ImageUrl)
            .Must(BeValidUrl).WithMessage("ImageUrl phải là URL hợp lệ")
            .When(x => !string.IsNullOrEmpty(x.ImageUrl));
    }

    private bool BeValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) 
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
