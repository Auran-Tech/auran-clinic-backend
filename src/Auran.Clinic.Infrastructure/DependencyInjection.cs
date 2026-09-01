using System.Text;
using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Infrastructure.Authentication;
using Auran.Clinic.Infrastructure.Authorization;
using Auran.Clinic.Infrastructure.Caching;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
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
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<AuranClinicDbContext>(options => options.UseSqlServer(connectionString));
            services.AddScoped<PermissionCatalogInitializer>();
            services.AddHostedService<PermissionCatalogHostedService>();
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
                            context.Fail("The user or clinic is inactive.");
                        }
                    }
                };
            });

        services.AddAuthorization();
        services.AddHttpContextAccessor();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEffectivePermissionService, EffectivePermissionService>();
        services.AddScoped<ICurrentUserContext, CurrentUser>();
        services.AddScoped<AccessTokenStateValidator>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuranCaching();
        return services;
    }
}
