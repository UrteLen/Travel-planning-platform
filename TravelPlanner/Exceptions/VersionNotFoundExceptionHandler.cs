using Microsoft.AspNetCore.Diagnostics;

namespace TravelPlanner.Exceptions;

public class VersionNotFoundExceptionHandler : IExceptionHandler
{
    private readonly ILogger<VersionNotFoundExceptionHandler> _logger;

    public VersionNotFoundExceptionHandler(ILogger<VersionNotFoundExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not VersionNotFoundException notFound)
        {
            return false;
        }

        _logger.LogWarning(
            "Version {VersionNumber} was not found for trip {TripId}",
            notFound.VersionNumber,
            notFound.TripId);

        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
        await httpContext.Response.WriteAsJsonAsync(
            new
            {
                error = "Version not found",
                tripId = notFound.TripId,
                versionNumber = notFound.VersionNumber
            },
            cancellationToken);

        return true;
    }
}