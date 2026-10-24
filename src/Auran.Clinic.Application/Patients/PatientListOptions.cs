namespace Auran.Clinic.Application.Patients;

public static class PatientListOptions
{
    public const string Name = "name";
    public const string PatientNumber = "patientNumber";
    public const string Phone = "phone";
    public const string Gender = "gender";
    public const string CreatedDate = "createdDate";
    public const string Male = "Male";
    public const string Female = "Female";
    public const string Unspecified = "Unspecified";

    public static readonly IReadOnlySet<string> SortFields =
        new HashSet<string>(StringComparer.Ordinal)
        {
            Name, PatientNumber, Phone, Gender, CreatedDate
        };

    public static readonly IReadOnlySet<string> GenderValues =
        new HashSet<string>(StringComparer.Ordinal)
        {
            Male, Female, Unspecified
        };
}
