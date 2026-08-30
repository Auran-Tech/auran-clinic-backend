using System.Text;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Clinics;
using Auran.Clinic.Application.Codes;
using Auran.Clinic.Application.Features;
using Auran.Clinic.Application.Files;
using Auran.Clinic.Application.Users;
using Auran.Clinic.Infrastructure.Auditing;
using Auran.Clinic.Infrastructure.Authentication;
using Auran.Clinic.Infrastructure.Authorization;
using Auran.Clinic.Infrastructure.Caching;
using Auran.Clinic.Infrastructure.Clinics;
using Auran.Clinic.Infrastructure.Codes;
using Auran.Clinic.Infrastructure.Features;
using Auran.Clinic.Infrastructure.Files;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Auran.Clinic.Infrastructure.Platform;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Auran.Clinic.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<AuditSaveChangesInterceptor>();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<AuranClinicDbContext>((serviceProvider, options) =>
                options.UseSqlServer(connectionString)
                    .AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>()));
        }

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

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "Jwt:Issuer is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "Jwt:Audience is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.SigningKey) && options.SigningKey.Length >= 32,
                "Jwt:SigningKey must be configured with at least 32 characters.")
            .Validate(options => options.AccessTokenMinutes is > 0 and <= 60,
                "Jwt:AccessTokenMinutes must be between 1 and 60.")
            .Validate(options => options.RefreshTokenDays is > 0 and <= 90,
                "Jwt:RefreshTokenDays must be between 1 and 90.")
            .ValidateOnStart();

        services.Configure<PlatformBootstrapOptions>(configuration.GetSection(PlatformBootstrapOptions.SectionName));
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt configuration is required.");
        if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
            throw new InvalidOperationException("Jwt:SigningKey must be configured with at least 32 characters.");

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
                    OnMessageReceived = context =>
                    {
                        var normalizedToken = BearerTokenNormalizer.NormalizeAuthorizationHeader(
                            context.Request.Headers.Authorization.ToString());

                        if (!string.IsNullOrWhiteSpace(normalizedToken))
                            context.Token = normalizedToken;

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        var validator = context.HttpContext.RequestServices
                            .GetRequiredService<AccessSessionValidator>();

                        if (context.Principal is null
                            || !await validator.IsActiveAsync(
                                context.Principal,
                                context.HttpContext.RequestAborted))
                        {
                            context.Fail("Authentication session is no longer active.");
                        }
                    }
                };
            });

        services.AddAuthorization();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPlatformAuthService, PlatformAuthService>();
        services.AddScoped<AccessSessionValidator>();
        services.AddScoped<ICurrentActor, CurrentActor>();
        services.AddScoped<IClinicService, ClinicService>();
        services.AddScoped<IPlatformClinicService, PlatformClinicService>();
        services.AddScoped<ICodeGeneratorService, CodeGeneratorService>();
        services.AddScoped<IFileService, FileService>();
        services.AddSingleton<IFileStorageProvider, LocalFileStorageProvider>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IClinicAccessService, ClinicAccessService>();
        services.AddScoped<IUserAccountService, UserAccountService>();
        services.AddScoped<IPermissionCatalogService, PermissionCatalogService>();
        services.AddScoped<SystemCatalogService>();
        services.AddScoped<PlatformBootstrapService>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, FeatureAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, ClinicActorAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, PlatformActorAuthorizationHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuditAuthorizationMiddlewareResultHandler>();
        services.AddAuranCaching();
        return services;
    }
}
