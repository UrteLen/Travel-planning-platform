namespace TravelPlanner.Models;

public record PlannedVisit(Place Place, DateTime ArrivalTime, DateTime DepartureTime)
    : IComparable<PlannedVisit>
{
    public int CompareTo(PlannedVisit? other)
    {
        if (other is null)
        {
            return 1;
        }

        int byArrival = ArrivalTime.CompareTo(other.ArrivalTime);
        return byArrival != 0 ? byArrival : DepartureTime.CompareTo(other.DepartureTime);
    }
}