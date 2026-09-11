using System.Net;
using System.Threading.RateLimiting;
using Auran.Clinic.Api.HealthChecks;
using Auran.Clinic.Api.Infrastructure;
using Auran.Clinic.Api.Localization;
using Auran.Clinic.Api.OpenApi;
using Auran.Clinic.Api.Validation;
using Auran.Clinic.Application;
using Auran.Clinic.Application.Localization;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddApiLocalization();
builder.Services.AddControllers(options =>
    options.Filters.AddService<ApiResponseMessageFilter>());
builder.Services.AddApiValidation();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Auran Clinic API",
        Version = "v1",
        Description = "Backend API for Auran Clinic. The OpenAPI document is the machine-readable source of truth for API discovery and AI tool generation. Operation IDs are stable identifiers and endpoint descriptions document authentication, side effects, and expected responses."
    });

    options.EnableAnnotations();
    options.OperationFilter<AllowAnonymousOperationFilter>();
    options.OperationFilter<AuthorizationResponsesOperationFilter>();
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT access token returned by the login or refresh endpoint. Enter the token only; Swagger adds the Bearer prefix."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        }] = Array.Empty<string>()
    });
});

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(ApiSecurityPolicies.FrontendCors, policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();

    var configuredProxies = builder.Configuration
        .GetSection("ForwardedHeaders:KnownProxies")
        .Get<string[]>() ?? [];

    foreach (var configuredProxy in configuredProxies)
    {
        if (IPAddress.TryParse(configuredProxy, out var proxyAddress))
            options.KnownProxies.Add(proxyAddress);
    }
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        var localizer = context.HttpContext.RequestServices
            .GetRequiredService<IStringLocalizer<ApiMessages>>();

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(
            new BaseResponse
            {
                Status = false,
                Message = localizer[ApiMessageKeys.TooManyRequests].Value,
                Error = "rate_limit_exceeded"
            },
            cancellationToken);
    };
    options.AddPolicy(ApiSecurityPolicies.LoginRateLimit, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            }));
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services
    .AddHealthChecks()
    .AddCheck<DatabaseReadinessHealthCheck>("database", tags: ["ready"]);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseForwardedHeaders();
app.UseRequestLocalization();
app.UseExceptionHandler();
app.UseStatusCodePages(async statusCodeContext =>
{
    var httpContext = statusCodeContext.HttpContext;
    if (httpContext.Response.HasStarted)
        return;

    var (messageKey, error) = httpContext.Response.StatusCode switch
    {
        StatusCodes.Status401Unauthorized => (ApiMessageKeys.Unauthorized, "unauthorized"),
        StatusCodes.Status403Forbidden => (ApiMessageKeys.Forbidden, "forbidden"),
        StatusCodes.Status404NotFound => (ApiMessageKeys.NotFound, "not_found"),
        StatusCodes.Status429TooManyRequests => (ApiMessageKeys.TooManyRequests, "rate_limit_exceeded"),
        >= StatusCodes.Status500InternalServerError => (ApiMessageKeys.InternalServerError, "internal_server_error"),
        _ => (ApiMessageKeys.BadRequest, "request_failed")
    };

    var localizer = httpContext.RequestServices.GetRequiredService<IStringLocalizer<ApiMessages>>();
    httpContext.Response.ContentType = "application/json";
    await httpContext.Response.WriteAsJsonAsync(new BaseResponse
    {
        Status = false,
        Message = localizer[messageKey].Value,
        Error = error
    });
});
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Correlation-ID"] = context.TraceIdentifier;
    await next();
});
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        diagnosticContext.Set("TraceIdentifier", httpContext.TraceIdentifier);
});

// Keep the machine-readable OpenAPI document available in every environment so
// automated clients can discover the API contract. The interactive UI remains development-only.
app.UseSwagger();

if (app.Environment.IsDevelopment())
    app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors(ApiSecurityPolicies.FrontendCors);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});
app.Run();

public partial class Program;
