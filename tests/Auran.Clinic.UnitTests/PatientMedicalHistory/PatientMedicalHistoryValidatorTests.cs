using Auran.Clinic.Application.PatientMedicalHistory;

namespace Auran.Clinic.UnitTests.PatientMedicalHistory;

public sealed class PatientMedicalHistoryValidatorTests
{
    [Fact]
    public void Condition_requires_patient_and_name()
    {
        var validator = new CreatePatientConditionRequestValidator();

        var result = validator.Validate(new CreatePatientConditionRequest());

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Allergy_accepts_reaction_and_notes()
    {
        var validator = new CreatePatientAllergyRequestValidator();

        var result = validator.Validate(new CreatePatientAllergyRequest
        {
            PatientId = Guid.NewGuid(),
            Name = "Penicillin",
            Reaction = "Rash",
            Notes = "Recorded by patient"
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Medication_rejects_overlong_dosage()
    {
        var validator = new CreatePatientMedicationRequestValidator();

        var result = validator.Validate(new CreatePatientMedicationRequest
        {
            PatientId = Guid.NewGuid(),
            Name = "Medication",
            Dosage = new string('A', 501)
        });

        Assert.False(result.IsValid);
    }
}
