using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Auran.Clinic.Api.OpenApi;

public sealed class AllowAnonymousOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var actionAllowsAnonymous = context.MethodInfo
            .GetCustomAttributes(inherit: true)
            .OfType<AllowAnonymousAttribute>()
            .Any();

        var controllerAllowsAnonymous = context.MethodInfo.DeclaringType?
            .GetCustomAttributes(inherit: true)
            .OfType<AllowAnonymousAttribute>()
            .Any() == true;

        if (actionAllowsAnonymous || controllerAllowsAnonymous)
            operation.Security = new List<OpenApiSecurityRequirement>();
    }
}
