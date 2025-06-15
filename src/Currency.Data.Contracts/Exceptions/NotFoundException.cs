namespace Currency.Data.Contracts.Exceptions;

public class NotFoundException(string message) : Exception(message)
{
    public static T Throw<T>(string message)
    {
        throw new NotFoundException(message);
    }
}