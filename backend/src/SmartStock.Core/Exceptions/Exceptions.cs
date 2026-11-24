using SmartStock.Core.Common;

namespace SmartStock.Core.Exceptions;

public class AppException : Exception
{
    public int StatusCode { get; }
    public List<ValidationError>? Errors { get; }

    public AppException(string message, int statusCode = 400, List<ValidationError>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string resourceName, object key)
        : base($"{resourceName} with identifier '{key}' was not found.", 404)
    {
    }

    public NotFoundException(string message) : base(message, 404)
    {
    }
}

public class ValidationAppException : AppException
{
    public ValidationAppException(string message, List<ValidationError> errors)
        : base(message, 400, errors)
    {
    }

    public ValidationAppException(string field, string error)
        : base("Validation failed.", 400, [new ValidationError(field, error)])
    {
    }
}

public class UnauthorizedAppException : AppException
{
    public UnauthorizedAppException(string message = "Unauthorized access.")
        : base(message, 401)
    {
    }
}

public class ForbiddenAppException : AppException
{
    public ForbiddenAppException(string message = "You do not have permission to access this resource.")
        : base(message, 403)
    {
    }
}

public class ConflictAppException : AppException
{
    public ConflictAppException(string message)
        : base(message, 409)
    {
    }
}

public class InsufficientStockException : AppException
{
    public InsufficientStockException(string productName, decimal requested, decimal available)
        : base($"Insufficient stock for '{productName}'. Requested: {requested}, Available: {available}.", 400)
    {
    }
}

public class NotFoundAppException : NotFoundException
{
    public NotFoundAppException(string resourceName, object key) : base(resourceName, key) { }
    public NotFoundAppException(string message) : base(message) { }
}

public class DuplicateEntityException : ConflictAppException
{
    public DuplicateEntityException(string entityName, string field, object value)
        : base($"{entityName} with {field} '{value}' already exists.") { }
}
