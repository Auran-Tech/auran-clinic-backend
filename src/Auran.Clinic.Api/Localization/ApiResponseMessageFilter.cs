using Auran.Clinic.Application.Localization;
using Auran.Clinic.Application.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Localization;

namespace Auran.Clinic.Api.Localization;

public sealed class ApiResponseMessageFilter(IStringLocalizer<ApiMessages> localizer) : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult { Value: BaseResponse response } objectResult)
        {
            var statusCode = objectResult.StatusCode ?? context.HttpContext.Response.StatusCode;
            var messageKey = ApiResponseMessageSelector.Select(context.HttpContext, response, statusCode);
            response.Message = localizer[messageKey].Value;
        }

        await next();
    }
}
