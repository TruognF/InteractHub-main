using FluentValidation;
using InteractHub.API.DTOs;

namespace InteractHub.API.DTOs.Validators;

public class UpdateNotificationValidator : AbstractValidator<UpdateNotificationDto>
{
    public UpdateNotificationValidator()
    {
        // IsRead là boolean, không cần validation đặc biệt
    }
}
