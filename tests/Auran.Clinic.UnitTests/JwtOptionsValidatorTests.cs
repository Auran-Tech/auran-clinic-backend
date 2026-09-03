using Auran.Clinic.Infrastructure;
using Auran.Clinic.Infrastructure.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Auran.Clinic.UnitTests;

public class JwtOptionsValidatorTests
{
    private readonly JwtOptionsValidator _validator = new();

    [Fact]
    public void Validate_AcceptsStrongConfiguration()
    {
        var options = CreateOptions("Auran_Test_Signing_Key_With_At_Least_32_Bytes");

        var result = _validator.Validate(Options.DefaultName, options);

        Assert.Same(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void Validate_RejectsMissingSigningKey()
    {
        var options = CreateOptions(string.Empty);

        var result = _validator.Validate(Options.DefaultName, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("SigningKey is required", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_RejectsShortSigningKey()
    {
        var options = CreateOptions("too-short");

        var result = _validator.Validate(Options.DefaultName, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("at least 32 bytes", StringComparison.Ordinal));
    }

    [Fact]
    public void AddInfrastructure_FailsFastWhenSigningKeyIsMissing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=AuranClinicUnitTests;User Id=sa;Password=Unit_Test_Only_123!;TrustServerCertificate=True;Encrypt=False",
                ["Jwt:Issuer"] = "Auran.Clinic",
                ["Jwt:Audience"] = "Auran.Clinic.Client",
                ["Jwt:AccessTokenMinutes"] = "60",
                ["Jwt:RefreshTokenDays"] = "60"
            })
            .Build();

        var services = new ServiceCollection();

        var exception = Assert.Throws<OptionsValidationException>(
            () => services.AddInfrastructure(configuration));

        Assert.Contains(
            exception.Failures,
            failure => failure.Contains("SigningKey is required", StringComparison.Ordinal));
    }

    private static JwtOptions CreateOptions(string signingKey)
    {
        return new JwtOptions
        {
            Issuer = "Auran.Clinic",
            Audience = "Auran.Clinic.Client",
            SigningKey = signingKey,
            AccessTokenMinutes = 60,
            RefreshTokenDays = 60
        };
    }
}
