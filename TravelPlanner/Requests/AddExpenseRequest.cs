namespace TravelPlanner.Requests;
public class AddExpenseRequest
    {
        public decimal Amount { get; set; }
        public required string Category { get; set; }
        public Guid ParticipantId { get; set; }
        public required string TripCode { get; set; }
    }