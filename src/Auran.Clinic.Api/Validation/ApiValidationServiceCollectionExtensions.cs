using Auran.Clinic.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace Auran.Clinic.Api.Validation;

public static class ApiValidationServiceCollectionExtensions
{
    public static IServiceCollection AddApiValidation(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .SelectMany(entry => entry.Value?.Errors.Select(error =>
                        string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? $"{entry.Key} is invalid."
                            : error.ErrorMessage) ?? Array.Empty<string>())
                    .Where(error => !string.IsNullOrWhiteSpace(error))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();

                return new BadRequestObjectResult(new BaseResponse
                {
                    Status = false,
                    Message = "Validation failed.",
                    Error = string.Join(" ", errors)
                });
            };
        });

        return services;
    }
}
