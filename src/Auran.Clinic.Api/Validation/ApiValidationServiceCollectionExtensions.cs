using Auran.Clinic.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace Auran.Clinic.Api.Validation;

public static class ApiValidationServiceCollectionExtensions
{
    public static IServiceCollection AddApiValidation(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = _ =>
                new BadRequestObjectResult(new BaseResponse
                {
                    Status = false,
                    Error = "validation_error"
                });
        });

        return services;
    }
}
