using System.Text;
using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Attachments;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Clinics;
using Auran.Clinic.Application.Codes;
using Auran.Clinic.Application.ClinicalSessions;
using Auran.Clinic.Application.ClinicalOrders;
using Auran.Clinic.Application.ClinicalMeasurements;
using Auran.Clinic.Application.Files;
using Auran.Clinic.Application.FollowUps;
using Auran.Clinic.Application.Lookups;
using Auran.Clinic.Application.Patients;
using Auran.Clinic.Application.PatientProfiles;
using Auran.Clinic.Application.PendingDocumentation;
using Auran.Clinic.Application.Queue;
using Auran.Clinic.Application.Reporting;
using Auran.Clinic.Application.Settings;
using Auran.Clinic.Application.Users;
using Auran.Clinic.Application.Workflow;
using Auran.Clinic.Application.Visits;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Auditing;
using Auran.Clinic.Infrastructure.Attachments;
using Auran.Clinic.Infrastructure.Authentication;
using Auran.Clinic.Infrastructure.Authorization;
using Auran.Clinic.Infrastructure.Caching;
using Auran.Clinic.Infrastructure.Clinics;
using Auran.Clinic.Infrastructure.Codes;
using Auran.Clinic.Infrastructure.ClinicalSessions;
using Auran.Clinic.Infrastructure.ClinicalOrders;
using Auran.Clinic.Infrastructure.ClinicalMeasurements;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Files;
using Auran.Clinic.Infrastructure.FollowUps;
using Auran.Clinic.Infrastructure.Lookups;
using Auran.Clinic.Infrastructure.Persistence;
using Auran.Clinic.Infrastructure.Patients;
using Auran.Clinic.Infrastructure.PatientProfiles;
using Auran.Clinic.Infrastructure.PendingDocumentation;
using Auran.Clinic.Infrastructure.Queue;
using Auran.Clinic.Infrastructure.Reporting;
using Auran.Clinic.Infrastructure.Settings;
using Auran.Clinic.Infrastructure.Platform;
using Auran.Clinic.Infrastructure.Users;
using Auran.Clinic.Infrastructure.Workflow;
using Auran.Clinic.Infrastructure.Visits;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Auran.Clinic.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ClinicScopeOverride>();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");

        services.AddDbContext<AuranClinicDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<PermissionCatalogInitializer>();
        services.AddHostedService<PermissionCatalogHostedService>();
        services.AddScoped<PlatformBootstrapService>();
        services.AddHostedService<PlatformBootstrapHostedService>();

        services.Configure<PlatformBootstrapOptions>(
            configuration.GetSection(PlatformBootstrapOptions.SectionName));

        services.AddIdentityCore<ApplicationIdentityUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddEntityFrameworkStores<AuranClinicDbContext>()
        .AddSignInManager();

        var jwtSection = configuration.GetRequiredSection(JwtOptions.SectionName);
        var jwt = jwtSection.Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt configuration is required.");

        var jwtValidator = new JwtOptionsValidator();
        var jwtValidation = jwtValidator.Validate(Options.DefaultName, jwt);
        if (jwtValidation.Failed)
        {
            throw new OptionsValidationException(
                Options.DefaultName,
                typeof(JwtOptions),
                jwtValidation.Failures);
        }

        services.AddSingleton<IValidateOptions<JwtOptions>>(jwtValidator);
        services.AddOptions<JwtOptions>()
            .Bind(jwtSection)
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        if (context.Principal is null)
                        {
                            context.Fail("Authenticated principal is missing.");
                            return;
                        }

                        var validator = context.HttpContext.RequestServices
                            .GetRequiredService<AccessTokenStateValidator>();
                        if (!await validator.IsActiveAsync(
                                context.Principal,
                                context.HttpContext.RequestAborted))
                        {
                            context.Fail("The authentication session is inactive.");
                        }
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                ActorPolicies.Clinic,
                policy => policy
                    .RequireAuthenticatedUser()
                    .RequireClaim("actor_type", ActorType.Clinic.ToString())
                    .RequireClaim("clinic_id"));

            options.AddPolicy(
                ActorPolicies.Platform,
                policy => policy
                    .RequireAuthenticatedUser()
                    .RequireClaim("actor_type", ActorType.Platform.ToString()));
        });
        services.AddHttpContextAccessor();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPlatformAuthService, PlatformAuthService>();
        services.AddScoped<IPlatformClinicService, PlatformClinicService>();
        services.AddSingleton<ISystemLookupService, SystemLookupService>();
        services.AddScoped<IEffectivePermissionService, EffectivePermissionService>();
        services.AddScoped<IPermissionCatalogService, PermissionCatalogService>();
        services.AddScoped<IUserAccountService, UserAccountService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IVisitService, VisitService>();
        services.AddScoped<IQueueService, QueueService>();
        services.AddScoped<IClinicalSessionService, ClinicalSessionService>();
        services.AddScoped<IClinicalOrderService, ClinicalOrderService>();
        services.AddScoped<IClinicalMeasurementService, ClinicalMeasurementService>();
        services.AddScoped<IClinicalFieldConfigurationService, ClinicalFieldConfigurationService>();
        services.Configure<FileStorageOptions>(
            configuration.GetSection(FileStorageOptions.SectionName));
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IPatientAttachmentService, PatientAttachmentService>();
        services.AddScoped<IPendingDocumentationService, PendingDocumentationService>();
        services.AddScoped<IPatientProfileService, PatientProfileService>();
        services.AddScoped<IPatientProfileConfigurationService, PatientProfileConfigurationService>();
        services.AddScoped<IFollowUpService, FollowUpService>();
        services.AddScoped<IClinicSettingsService, ClinicSettingsService>();
        services.AddScoped<IReportingService, ReportingService>();
        services.AddScoped<IWorkflowConfigurationService, WorkflowConfigurationService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuditReadService, AuditReadService>();
        services.AddScoped<ICodeGeneratorService, CodeGeneratorService>();
        services.AddScoped<ICurrentUserContext, CurrentUser>();
        services.AddScoped<AccessTokenStateValidator>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuranCaching();
        return services;
    }
}
