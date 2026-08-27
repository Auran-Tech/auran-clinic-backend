namespace Auran.Clinic.Application.Features;

public static class SystemFeatureCatalog
{
    public static readonly IReadOnlyCollection<FeatureDefinition> All = new[]
    {
        new FeatureDefinition(Features.Patients, "Patients", "Patient registration and patient records.", true),
        new FeatureDefinition(Features.DynamicPatientProfile, "Dynamic Patient Profile", "Configurable patient profile sections and fields.", true),
        new FeatureDefinition(Features.Queue, "Live Queue", "Clinic workflow and live patient queue.", true),
        new FeatureDefinition(Features.Visits, "Visits", "Visits and doctor sessions.", true),
        new FeatureDefinition(Features.ClinicalOrders, "Clinical Orders", "Prescription, medication, lab, radiology and procedure orders.", true),
        new FeatureDefinition(Features.FollowUps, "Follow-ups", "Follow-up recommendations and tracking.", true),
        new FeatureDefinition(Features.Reports, "Reports", "Clinic reports and exports.", true),
        new FeatureDefinition(Features.AdvancedReports, "Advanced Reports", "Advanced reporting and analytics.", false),
        new FeatureDefinition(Features.Ai, "AI Features", "AURAN AI-assisted clinic capabilities.", false)
    };
}
