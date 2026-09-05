namespace UserService.Application.Common;

public enum ServiceErrorType
{
    Validation,
    Conflict,
    Unauthorized,
    NotFound
}

public record ServiceError(ServiceErrorType Type, string Message);

public class ServiceResult<T>
{
    public bool Succeeded { get; }
    public T? Value { get; }
    public ServiceError? Error { get; }

    private ServiceResult(bool succeeded, T? value, ServiceError? error)
    {
        Succeeded = succeeded;
        Value = value;
        Error = error;
    }

    public static ServiceResult<T> Success(T value) => new(true, value, null);

    public static ServiceResult<T> Failure(ServiceErrorType type, string message) =>
        new(false, default, new ServiceError(type, message));
}
