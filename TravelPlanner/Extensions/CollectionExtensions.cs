using TravelPlanner.Models;

namespace TravelPlanner.Extensions;

public static class CollectionExtensions
{
    public static T? FindById<T>(this IEnumerable<T> items, Guid id) where T : class, IHasId
    {
        return items.FirstOrDefault(item => item.Id == id);
    }
}