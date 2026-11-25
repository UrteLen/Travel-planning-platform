using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using TravelPlanner.Exceptions;

namespace TravelPlanner.UnitTests;

public class VersionNotFoundExceptionHandlerTests
{
    private readonly VersionNotFoundExceptionHandler _handler =
        new(NullLogger<VersionNotFoundExceptionHandler>.Instance);

    [Fact]
    public async Task TryHandleAsync_VersionNotFound_Returns404WithTripAndVersion()
    {
        var tripId = Guid.NewGuid();
        var context = CreateContext();

        var handled = await _handler.TryHandleAsync(
            context, new VersionNotFoundException(tripId, 5), TestContext.Current.CancellationToken);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        Assert.Contains("Version not found", body);
        Assert.Contains(tripId.ToString(), body);
        Assert.Contains("5", body);
    }

    [Fact]
    public async Task TryHandleAsync_OtherException_ReturnsFalse()
    {
        var context = CreateContext();

        var handled = await _handler.TryHandleAsync(
            context, new InvalidOperationException("other"), TestContext.Current.CancellationToken);

        Assert.False(handled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }
}
