namespace TravelPlanner.Requests;
    public class SetBudgetLimitsRequest
    {
        public string TripCode { get; set; } = string.Empty;
        public Dictionary<string, decimal> CategoryLimits { get; set; } = new();
    }