namespace TravelPlanner.Exceptions;

public class TooManyLoginAttemptsException : Exception
{
    public TooManyLoginAttemptsException()
        : base("Too many login attempts. Try again later.")
    {
    }
}