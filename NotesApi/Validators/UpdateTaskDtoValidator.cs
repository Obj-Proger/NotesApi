using FluentValidation;
using NotesApi.Dtos.Tasks;

namespace NotesApi.Validators;

/// <summary>
/// Validates UpdateTaskDto input for task update requests.
/// Supports partial updates with optional fields validation.
/// Only validates fields that are explicitly provided.
/// </summary>
public class UpdateTaskDtoValidator : AbstractValidator<UpdateTaskDto>
{
    private const int TitleMaxLength = 500;
    private const int DescriptionMaxLength = 2000;

    public UpdateTaskDtoValidator()
    {
        // Title must not be empty if provided and must not exceed maximum length.
        // Unless clause allows null/empty values for partial updates.
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title must not be empty if provided.")
            .MaximumLength(TitleMaxLength).WithMessage($"Title must not exceed {TitleMaxLength} characters.")
            .Unless(x => string.IsNullOrEmpty(x.Title));

        // Description is optional but must not exceed maximum length if provided.
        RuleFor(x => x.Description)
            .MaximumLength(DescriptionMaxLength).WithMessage($"Description must not exceed {DescriptionMaxLength} characters.")
            .Unless(x => string.IsNullOrEmpty(x.Description));

        // Priority must be a valid enum value if provided.
        // When clause ensures validation only occurs when Priority has an explicit value.
        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority value.")
            .When(x => x.Priority.HasValue);
    }
}