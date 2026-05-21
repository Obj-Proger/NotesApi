using FluentValidation;
using NotesApi.Dtos.Users;

namespace NotesApi.Validators;

/// <summary>
/// Validates UpdateUserDto input for user profile update requests.
/// Supports partial updates with optional fields validation.
/// Only validates fields that are explicitly provided.
/// </summary>
public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    private const int EmailMaxLength = 255;
    private const int NameMaxLength = 100;

    public UpdateUserDtoValidator()
    {
        // Email must be a valid email format if provided.
        // Email validation ensures database constraints are met for authentication purposes.
        // Unless clause allows null/empty values for partial updates.
        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(EmailMaxLength).WithMessage($"Email must not exceed {EmailMaxLength} characters.")
            .Unless(x => string.IsNullOrEmpty(x.Email));

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