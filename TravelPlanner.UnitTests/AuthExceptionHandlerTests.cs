using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using TravelPlanner.Exceptions;

namespace TravelPlanner.UnitTests;

public class AuthExceptionHandlerTests
{
    private readonly AuthExceptionHandler _handler = new(NullLogger<AuthExceptionHandler>.Instance);

    [Fact]
    public async Task TryHandleAsync_InvalidCredentials_Returns401()
    {
        var context = CreateContext();

        var handled = await _handler.TryHandleAsync(context, new InvalidCredentialsException(), TestContext.Current.CancellationToken);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Contains("Invalid email or password", await ReadBodyAsync(context));
    }

    [Fact]
    public async Task TryHandleAsync_EmailAlreadyRegistered_Returns409()
    {
        var context = CreateContext();

        var handled = await _handler.TryHandleAsync(context, new EmailAlreadyRegisteredException("user@example.com"), TestContext.Current.CancellationToken);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Contains("already registered", await ReadBodyAsync(context));
    }

    [Fact]
    public async Task TryHandleAsync_TooManyLoginAttempts_Returns429()
    {
        var context = CreateContext();

        var handled = await _handler.TryHandleAsync(context, new TooManyLoginAttemptsException(), TestContext.Current.CancellationToken);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_UnknownException_ReturnsFalseAndLeavesResponseAlone()
    {
        var context = CreateContext();

        var handled = await _handler.TryHandleAsync(context, new InvalidOperationException("other"), TestContext.Current.CancellationToken);

        Assert.False(handled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadBodyAsync(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }
}
