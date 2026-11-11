using TravelPlanner.Enums;

namespace TravelPlanner.Models;

public class ChecklistItem
{
    public const int MaxTextLength = 100;

    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ParticipantId { get; init; }
    public string Text { get; private set; } = string.Empty;
    public ChecklistCategory Category { get; private set; } = ChecklistCategory.Other;
    public bool IsDone { get; private set; }

    public ChecklistItem(
        Guid participantId,
        string text,
        ChecklistCategory category = ChecklistCategory.Other)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Checklist item text cannot be empty.", nameof(text));
        }

        if (!Enum.IsDefined(category))
        {
            throw new ArgumentException("Unknown checklist category.", nameof(category));
        }

        var trimmedText = text.Trim();
        if (trimmedText.Length > MaxTextLength)
        {
            throw new ArgumentException(
                $"Checklist item text cannot be longer than {MaxTextLength} characters.",
                nameof(text));
        }

        ParticipantId = participantId;
        Text = trimmedText;
        Category = category;
    }

    public void MarkDone()
    {
        IsDone = true;
    }

    public void MarkNotDone()
    {
        IsDone = false;
    }

    private ChecklistItem()
    {
    }
}