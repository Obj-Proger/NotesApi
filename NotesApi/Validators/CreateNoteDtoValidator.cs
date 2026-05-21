using FluentValidation;
using NotesApi.Dtos.Notes;

namespace NotesApi.Validators;

/// <summary>
/// Validates CreateNoteDto input for note creation requests.
/// Ensures title, content, and optional color are properly formatted.
/// </summary>
public class CreateNoteDtoValidator : AbstractValidator<CreateNoteDto>
{
    private const int TitleMaxLength = 500;
    private const string HexColorPattern = "^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$";

    public CreateNoteDtoValidator()
    {
        // Title is required and must not exceed maximum length.
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(TitleMaxLength).WithMessage($"Title must not exceed {TitleMaxLength} characters.");

        // Content is required and represents the main note body.
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.");

        // Color must be a valid hexadecimal color code if provided.
        // Accepts both 6-digit (#RRGGBB) and 3-digit (#RGB) formats.
        RuleFor(x => x.Color)
            .Matches(HexColorPattern)
            .When(x => !string.IsNullOrEmpty(x.Color))
            .WithMessage("Color must be a valid hex color code (e.g., #FF5733 or #FFF).");
    }
}