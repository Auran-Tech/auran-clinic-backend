using Auran.Clinic.Application.Models;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Auran.Clinic.Api.Filters;

public sealed class FluentValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values.Where(x => x is not null))
        {
            var model = argument!;
            var validatorType = typeof(IValidator<>).MakeGenericType(model.GetType());
            var validator = context.HttpContext.RequestServices.GetService(validatorType);
            if (validator is null)
                continue;

            dynamic dynamicValidator = validator;
            dynamic dynamicModel = model;
            var result = await dynamicValidator.ValidateAsync(
                dynamicModel,
                context.HttpContext.RequestAborted);

            if (result.IsValid)
                continue;

            context.Result = new BadRequestObjectResult(new BaseResponse
            {
                Status = false,
                Error = "VALIDATION_ERROR",
                Message = string.Join(" ", result.Errors.Select(x => x.ErrorMessage))
            });
            return;
        }

        await next();
    }
}
