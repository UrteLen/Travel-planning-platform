using System.ComponentModel.DataAnnotations;
using TravelPlanner.Enums;

namespace TravelPlanner.Requests;

public record VoteRequest([Required] VoteType? VoteType);
