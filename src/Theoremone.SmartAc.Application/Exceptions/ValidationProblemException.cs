namespace Theoremone.SmartAc.Application.Exceptions;
public class ValidationProblemException : Exception
{
    public ValidationProblemException(string key, string message) : base(message)
    {
        Key = key;
    }
    public string Key { get; }
}
