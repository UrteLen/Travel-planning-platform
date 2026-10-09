namespace TravelPlanner.Requests;
public class AddExpenseRequest
    {
        public decimal Amount { get; set; }
        public string Category { get; set; }
        public Guid ParticipantId { get; set; }
        public string TripCode { get; set; }
    }