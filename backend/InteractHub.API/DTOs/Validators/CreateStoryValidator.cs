using FluentValidation;
using InteractHub.API.DTOs;

namespace InteractHub.API.DTOs.Validators;

public class CreateStoryValidator : AbstractValidator<CreateStoryDto>
{
    public CreateStoryValidator()
    {
        RuleFor(x => x.ImageUrl)
            .Must(BeValidUrl).WithMessage("ImageUrl phải là URL hợp lệ")
            .When(x => !string.IsNullOrEmpty(x.ImageUrl));

        RuleFor(x => x.Content)
            .MaximumLength(500).WithMessage("Content không được vượt quá 500 ký tự");

        RuleFor(x => x.ExpireAt)
            .GreaterThan(DateTime.Now).WithMessage("ExpireAt phải là thời gian trong tương lai");
    }

    private bool BeValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) 
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
