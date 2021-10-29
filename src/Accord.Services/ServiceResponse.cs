using System;
using System.Threading.Tasks;

namespace Accord.Services;

public class ServiceResponse
{
    public bool Success { get; }
    public string ErrorMessage { get; }

    public bool Failure => !Success;

    protected ServiceResponse(bool success, string errorMessage)
    {
        if (success && !string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new ArgumentException("Cannot be successful with error message");
        }

        if (!success && string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new ArgumentException("Cannot be failure with no error message");
        }

        Success = success;
        ErrorMessage = errorMessage;
    }

    public static ServiceResponse Fail(string message)
    {
        return new ServiceResponse(false, message);
    }

    public static ServiceResponse<T> Fail<T>(string message)
    {
        return new ServiceResponse<T>(default(T), false, message);
    }

    public static ServiceResponse Ok()
    {
        return new ServiceResponse(true, string.Empty);
    }

    public static ServiceResponse<T> Ok<T>(T value)
    {
        return new ServiceResponse<T>(value, true, string.Empty);
    }

    public Task GetAction(Func<Task> onSuccessAction, Func<Task> onFailAction)
    {
        return Success ? onSuccessAction() : onFailAction();
    }
}

public class ServiceResponse<T> : ServiceResponse
{
    public T? Value { get; }

    protected internal ServiceResponse(T? value, bool success, string errorMessage) : base(success, errorMessage)
    {
        Value = value;
    }
}