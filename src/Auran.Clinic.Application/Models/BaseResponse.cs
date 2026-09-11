namespace Auran.Clinic.Application.Models;

public class BaseResponse
{
    public string Message { get; set; } = string.Empty;

    public bool Status { get; set; }

    public string? Error { get; set; }
}

public class BaseResponse<T> : BaseResponse where T : class
{
    public T? Data { get; set; }
}
