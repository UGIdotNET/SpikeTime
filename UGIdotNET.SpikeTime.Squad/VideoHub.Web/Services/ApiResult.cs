using System.Net;

namespace VideoHub.Web.Services;

public class ApiResult<T>
{
    public T? Value { get; init; }
    public HttpStatusCode? Status { get; init; }
    public string? Error { get; init; }
    public bool IsSuccess => Error is null;
    public bool IsNotFound => Status == HttpStatusCode.NotFound;

    public static ApiResult<T> Ok(T? value, HttpStatusCode status) => new() { Value = value, Status = status };

    public static ApiResult<T> Fail(HttpStatusCode? status, string error) => new() { Status = status, Error = error };
}
