namespace TravelPlanner.Exceptions;

public class DuplicateChecklistItemException : Exception
{
    public string Text { get; }

    public DuplicateChecklistItemException(string text)
        : base("This checklist item already exists.")
    {
        Text = text;
    }
}