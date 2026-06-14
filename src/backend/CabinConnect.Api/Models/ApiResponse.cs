namespace CabinConnect.Api.Models;

public sealed record ApiResponse<T>(T? Data, ApiError? Error)
{
    public static ApiResponse<T> Success(T data) => new(data, null);
    public static ApiResponse<T> Failure(ApiError error) => new(default, error);
}
