using FluentValidation;
using NotesApi.Dtos.Users;

namespace NotesApi.Validators;

/// <summary>
/// Validates CreateUserDto input for user registration requests.
/// Enforces username, email, and password complexity requirements.
/// </summary>
public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    private const int UsernameMinLength = 3;
    private const int UsernameMaxLength = 100;
    private const int EmailMaxLength = 255;
    private const int PasswordMinLength = 6;
    private const int NameMaxLength = 100;

    public CreateUserDtoValidator()
    {
        // Username is required and must be within specified length boundaries.
        // Length constraints help prevent database field overflow and improve UX.
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(UsernameMinLength).WithMessage($"Username must be at least {UsernameMinLength} characters long.")
            .MaximumLength(UsernameMaxLength).WithMessage($"Username must not exceed {UsernameMaxLength} characters.");

        // Email is required and must be a valid email format.
        // Email is used as unique identifier for authentication.
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(EmailMaxLength).WithMessage($"Email must not exceed {EmailMaxLength} characters.");

        // Password must meet minimum length requirement for account security.
        // Consider enforcing stronger requirements (uppercase, numbers, symbols) based on security policy.
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(PasswordMinLength).WithMessage($"Password must be at least {PasswordMinLength} characters long.");

        // First name is optional but must not exceed maximum length if provided.
        RuleFor(x => x.FirstName)
            .MaximumLength(NameMaxLength).WithMessage($"First name must not exceed {NameMaxLength} characters.")
            .Unless(x => string.IsNullOrEmpty(x.FirstName));

        // Last name is optional but must not exceed maximum length if provided.
        RuleFor(x => x.LastName)
            .MaximumLength(NameMaxLength).WithMessage($"Last name must not exceed {NameMaxLength} characters.")
            .Unless(x => string.IsNullOrEmpty(x.LastName));
    }
}