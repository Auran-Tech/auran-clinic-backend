using Auran.Clinic.Application.Settings;

namespace Auran.Clinic.UnitTests.Settings;

public sealed class ClinicSettingsValidatorTests
{
    [Fact]
    public void Update_requires_name_and_valid_reminder_window()
    {
        var validator = new UpdateClinicSettingsRequestValidator();

        var result = validator.Validate(new UpdateClinicSettingsRequest
        {
            ClinicName = "",
            DocumentationReminderHours = 0
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Update_accepts_basic_valid_settings()
    {
        var validator = new UpdateClinicSettingsRequestValidator();

        var result = validator.Validate(new UpdateClinicSettingsRequest
        {
            ClinicName = "Auran Clinic",
            DocumentationReminderHours = 12,
            Email = "clinic@example.com",
            PatientNumberPrefix = "PAT"
        });

        Assert.True(result.IsValid);
    }
}
