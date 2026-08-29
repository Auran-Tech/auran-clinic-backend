using Auran.Clinic.Api.OpenApi;
using Auran.Clinic.Application;
using Auran.Clinic.Infrastructure;
using Auran.Clinic.Infrastructure.Platform;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Auran Clinic API",
        Version = "v1",
        Description = "Backend API for Auran Clinic. The OpenAPI document is the machine-readable source of truth for API discovery and AI tool generation. Platform and clinic security scopes are intentionally separate; operation IDs are stable identifiers and endpoint descriptions document authentication, permissions and side effects."
    });

    options.EnableAnnotations();
    options.OperationFilter<AllowAnonymousOperationFilter>();
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT access token only (the value beginning with eyJ...). Do not type the 'Bearer ' prefix; Swagger adds it automatically. Platform and clinic JWTs are not interchangeable."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        }] = Array.Empty<string>()
    });
});
builder.Services.AddHealthChecks();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (builder.Configuration.GetValue<bool>($"{PlatformBootstrapOptions.SectionName}:Enabled"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<PlatformBootstrapService>().BootstrapAsync();
}

app.UseSwagger();
if (app.Environment.IsDevelopment())
    app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health/live");
app.Run();

public partial class Program;
