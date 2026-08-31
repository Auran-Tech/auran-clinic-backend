using System.Text;
using Microsoft.Extensions.Options;

namespace Auran.Clinic.Infrastructure.Authentication;

public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public const int MinimumSigningKeyBytes = 32;

    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            failures.Add("Jwt:Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add("Jwt:Audience is required.");
        }

        if (string.IsNullOrWhiteSpace(options.SigningKey))
        {
            failures.Add("Jwt:SigningKey is required and must be supplied by the deployment environment.");
        }
        else if (Encoding.UTF8.GetByteCount(options.SigningKey) < MinimumSigningKeyBytes)
        {
            failures.Add($"Jwt:SigningKey must be at least {MinimumSigningKeyBytes} bytes.");
        }

        if (options.AccessTokenMinutes <= 0)
        {
            failures.Add("Jwt:AccessTokenMinutes must be greater than zero.");
        }

        if (options.RefreshTokenDays <= 0)
        {
            failures.Add("Jwt:RefreshTokenDays must be greater than zero.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
