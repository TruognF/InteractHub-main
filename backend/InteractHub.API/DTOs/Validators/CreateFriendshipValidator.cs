using FluentValidation;
using InteractHub.API.DTOs;

namespace InteractHub.API.DTOs.Validators;

public class CreateFriendshipValidator : AbstractValidator<CreateFriendshipDto>
{
    public CreateFriendshipValidator()
    {
        RuleFor(x => x.FriendId)
            .NotEmpty().WithMessage("FriendId không được để trống");
    }
}
