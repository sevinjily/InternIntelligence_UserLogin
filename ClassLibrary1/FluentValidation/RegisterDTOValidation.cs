using Entities.DTOs;
using FluentValidation;

namespace Business.FluentValidation
{
    public class RegisterDTOValidation : AbstractValidator<RegisterDTO>
    {
        public RegisterDTOValidation()
        {
            RuleFor(x => x.FirstName)
                 .NotEmpty().WithMessage("Firstname cannot be empty")
                 .MaximumLength(50).WithMessage("Firstname cannot exceed 50 characters");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Lastname cannot be empty")
                .MaximumLength(50).WithMessage("Lastname cannot exceed 50 characters");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email cannot be empty")
                .EmailAddress().WithMessage("Invalid email format");

            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("Username cannot be empty")
                .MinimumLength(4).WithMessage("Username must be at least 4 characters long");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password cannot be empty")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Confirm password cannot be empty")
                .Equal(x => x.Password).WithMessage("Passwords do not match");
        }
    }
}
