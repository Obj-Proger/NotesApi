using FluentValidation;
using NotesApi.Dtos.Tasks;

namespace NotesApi.Validators;

/// <summary>
/// Validates CreateTaskDto input for task creation requests.
/// Ensures title, description length, and priority enum validity.
/// </summary>
public class CreateTaskDtoValidator : AbstractValidator<CreateTaskDto>
{
    private const int TitleMaxLength = 500;
    private const int DescriptionMaxLength = 2000;

    public CreateTaskDtoValidator()
    {
        // Title is required and must not exceed maximum length.
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(TitleMaxLength).WithMessage($"Title must not exceed {TitleMaxLength} characters.");

        // Description is optional but must not exceed maximum length if provided.
        RuleFor(x => x.Description)
            .MaximumLength(DescriptionMaxLength).WithMessage($"Description must not exceed {DescriptionMaxLength} characters.")
            .Unless(x => string.IsNullOrEmpty(x.Description));

        // Priority must be a valid enum value representing task urgency level.
        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority value.");
    }
}