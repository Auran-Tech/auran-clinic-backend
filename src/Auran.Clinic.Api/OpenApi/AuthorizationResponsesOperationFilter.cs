using Auran.Clinic.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Auran.Clinic.Api.OpenApi;

public sealed class AuthorizationResponsesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<IAllowAnonymous>().Any() || !metadata.OfType<IAuthorizeData>().Any())
            return;

        var schema = context.SchemaGenerator.GenerateSchema(typeof(BaseResponse), context.SchemaRepository);
        var content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/json"] = new() { Schema = schema }
        };

        operation.Responses.TryAdd("401", new OpenApiResponse
        {
            Description = "Unauthorized - authentication is missing, invalid, expired or the session has been revoked.",
            Content = content
        });
        operation.Responses.TryAdd("403", new OpenApiResponse
        {
            Description = "Forbidden - the authenticated actor does not satisfy the required actor scope, permission or feature policy.",
            Content = content
        });
    }
}
