namespace TravelPlanner.Exceptions;

public class EmailAlreadyRegisteredException : Exception
{
    public string Email { get; }

    public EmailAlreadyRegisteredException(string email)
        : base("Email is already registered.")
    {
        Email = email;
    }
}
