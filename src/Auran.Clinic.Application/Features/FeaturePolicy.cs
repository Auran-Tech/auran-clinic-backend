namespace Auran.Clinic.Application.Features;

public static class FeaturePolicy
{
    public const string Prefix = "ClinicFeature:";
    public static string For(string featureCode) => $"{Prefix}{featureCode}";
}
