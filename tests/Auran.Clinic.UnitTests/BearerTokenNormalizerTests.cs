using Auran.Clinic.Infrastructure.Authentication;

namespace Auran.Clinic.UnitTests;

public sealed class BearerTokenNormalizerTests
{
    private const string Jwt = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.signature";

    [Theory]
    [InlineData("Bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.signature")]
    [InlineData("Bearer   eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.signature  ")]
    [InlineData("Bearer Bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.signature")]
    [InlineData("Bearer \"eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.signature\"")]
    public void NormalizeAuthorizationHeader_ReturnsJwt(string authorizationHeader)
    {
        Assert.Equal(Jwt, BearerTokenNormalizer.NormalizeAuthorizationHeader(authorizationHeader));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Basic abc")]
    [InlineData("Bearer")]
    [InlineData("Bearer   ")]
    public void NormalizeAuthorizationHeader_InvalidInput_ReturnsNull(string? authorizationHeader)
    {
        Assert.Null(BearerTokenNormalizer.NormalizeAuthorizationHeader(authorizationHeader));
    }
}
