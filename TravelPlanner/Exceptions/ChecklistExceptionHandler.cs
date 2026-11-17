using Microsoft.AspNetCore.Diagnostics;

namespace TravelPlanner.Exceptions;

public class ChecklistExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ChecklistExceptionHandler> _logger;

    public ChecklistExceptionHandler(ILogger<ChecklistExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, message) = exception switch
        {
            NotTripParticipantException => (StatusCodes.Status403Forbidden, "You are not a participant of this trip"),
            ChecklistItemNotFoundException => (StatusCodes.Status404NotFound, "Checklist item not found"),
            DuplicateChecklistItemException => (StatusCodes.Status409Conflict, "This checklist item already exists"),
            _ => (0, string.Empty)
        };

        if (status == 0)
        {
            return false;
        }

        _logger.LogWarning("Checklist request failed: {ExceptionType}", exception.GetType().Name);

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(new { error = message }, cancellationToken);

        return true;
    }
}