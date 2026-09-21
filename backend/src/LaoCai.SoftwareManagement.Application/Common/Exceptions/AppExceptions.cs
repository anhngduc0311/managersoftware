namespace LaoCai.SoftwareManagement.Application.Common.Exceptions;

public class AppException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }

    public AppException(string message, string code = "system.error", int statusCode = 500)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string resourceName, object key)
        : base($"Không tìm thấy tài nguyên '{resourceName}' với mã ({key}).", "resource.not_found", 404)
    {
    }
}

public class ForbiddenException : AppException
{
    public ForbiddenException(string message = "Bạn không có quyền truy cập hoặc thực hiện thao tác này.")
        : base(message, "access.forbidden", 403)
    {
    }
}

public class ConcurrencyException : AppException
{
    public ConcurrencyException(string message = "Dữ liệu đã được cập nhật bởi một phiên làm việc khác. Vui lòng tải lại trang.")
        : base(message, "concurrency.version_mismatch", 412)
    {
    }
}

public class CustomValidationException : AppException
{
    public IDictionary<string, string[]> Errors { get; }

    public CustomValidationException(IDictionary<string, string[]> errors)
        : base("Một hoặc nhiều trường dữ liệu không hợp lệ.", "validation.failed", 400)
    {
        Errors = errors;
    }

    public CustomValidationException(string field, string message)
        : base("Dữ liệu không hợp lệ.", "validation.failed", 400)
    {
        Errors = new Dictionary<string, string[]>
        {
            { field, new[] { message } }
        };
    }
}

public class ConflictException : AppException
{
    public ConflictException(string message)
        : base(message, "resource.conflict", 409)
    {
    }
}

