using FluentValidation;
using InteractHub.API.DTOs;

namespace InteractHub.API.DTOs.Validators;

public class CreateLikeValidator : AbstractValidator<CreateLikeDto>
{
    public CreateLikeValidator()
    {
        RuleFor(x => x.PostId)
            .GreaterThan(0).WithMessage("PostId phải lớn hơn 0");
    }
}
