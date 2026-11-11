namespace TravelPlanner.Exceptions;

public class ChecklistItemNotFoundException : Exception
{
    public Guid ItemId { get; }

    public ChecklistItemNotFoundException(Guid itemId)
        : base("Checklist item was not found.")
    {
        ItemId = itemId;
    }
}