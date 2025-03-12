using Entities.DTOs;
using FluentValidation;

namespace Business.FluentValidation
{
    public class LoginDTOValidation:AbstractValidator<LoginDTO>
    {
        public LoginDTOValidation()
        {
            RuleFor(x => x.UsernameOrEmail)
                .NotEmpty().NotNull().WithMessage("Username or Email is required.");
               

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");
        }
    }
}

