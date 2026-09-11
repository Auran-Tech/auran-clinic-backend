using Auran.Clinic.Application.Localization;
using Auran.Clinic.Application.Models;

namespace Auran.Clinic.Api.Localization;

public static class ApiResponseMessageSelector
{
    public static string Select(HttpContext httpContext, BaseResponse response, int statusCode)
    {
        if (!response.Status)
        {
            if (string.Equals(response.Error, "validation_error", StringComparison.Ordinal))
                return ApiMessageKeys.ValidationFailed;

            return statusCode switch
            {
                StatusCodes.Status400BadRequest => ApiMessageKeys.BadRequest,
                StatusCodes.Status401Unauthorized => ApiMessageKeys.Unauthorized,
                StatusCodes.Status403Forbidden => ApiMessageKeys.Forbidden,
                StatusCodes.Status404NotFound => ApiMessageKeys.NotFound,
                StatusCodes.Status409Conflict => ApiMessageKeys.Conflict,
                StatusCodes.Status429TooManyRequests => ApiMessageKeys.TooManyRequests,
                >= StatusCodes.Status500InternalServerError => ApiMessageKeys.InternalServerError,
                _ => ApiMessageKeys.BadRequest
            };
        }

        if (statusCode == StatusCodes.Status201Created)
            return ApiMessageKeys.Created;

        return httpContext.Request.Method switch
        {
            "GET" => ApiMessageKeys.DataRetrieved,
            "PUT" or "PATCH" => ApiMessageKeys.Updated,
            "DELETE" => ApiMessageKeys.Deleted,
            _ => ApiMessageKeys.RequestSucceeded
        };
    }
}
