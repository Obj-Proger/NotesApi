using FluentValidation;
using NotesApi.Dtos.Auth;

namespace NotesApi.Validators;

/// <summary>
/// Validates LoginDto input for user authentication requests.
/// Ensures email and password are present and properly formatted.
/// </summary>
public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        // Email is required and must be a valid email format.
        // Generic error messages prevent account enumeration attacks.
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        // Password must not be empty.
        // Specific password complexity validation is deferred to authentication service.
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}