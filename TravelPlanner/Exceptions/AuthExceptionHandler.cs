using Microsoft.AspNetCore.Diagnostics;

namespace TravelPlanner.Exceptions;

public class AuthExceptionHandler : IExceptionHandler
{
    private readonly ILogger<AuthExceptionHandler> _logger;

    public AuthExceptionHandler(ILogger<AuthExceptionHandler> logger)
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
            InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "Invalid email or password"),
            EmailAlreadyRegisteredException => (StatusCodes.Status409Conflict, "Email is already registered"),
            _ => (0, string.Empty)
        };

        if (status == 0)
        {
            return false;
        }

        _logger.LogWarning("Authentication request failed: {ExceptionType}", exception.GetType().Name);

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(new { error = message }, cancellationToken);

        return true;
    }
}
