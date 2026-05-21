using FluentValidation;
using NotesApi.Dtos.Notes;

namespace NotesApi.Validators;

/// <summary>
/// Validates UpdateNoteDto input for note update requests.
/// Supports partial updates with optional fields validation.
/// Only validates fields that are explicitly provided.
/// </summary>
public class UpdateNoteDtoValidator : AbstractValidator<UpdateNoteDto>
{
    private const int TitleMaxLength = 500;
    private const string HexColorPattern = "^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$";

    public UpdateNoteDtoValidator()
    {
        // Title must not be empty if provided and must not exceed maximum length.
        // Unless clause allows null/empty values for partial updates.
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title must not be empty if provided.")
            .MaximumLength(TitleMaxLength).WithMessage($"Title must not exceed {TitleMaxLength} characters.")
            .Unless(x => string.IsNullOrEmpty(x.Title));

        // Color must be a valid hexadecimal color code if provided.
        // Accepts both 6-digit (#RRGGBB) and 3-digit (#RGB) formats.
        RuleFor(x => x.Color)
            .Matches(HexColorPattern)
            .When(x => !string.IsNullOrEmpty(x.Color))
            .WithMessage("Color must be a valid hex color code (e.g., #FF5733 or #FFF).");
    }
}