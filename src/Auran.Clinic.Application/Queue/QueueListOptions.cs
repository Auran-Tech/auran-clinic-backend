namespace Auran.Clinic.Application.Queue;

public static class QueueListOptions
{
    public static readonly TimeSpan RecentExitWindow = TimeSpan.FromHours(24);
    public const string Stage = "stage";
    public const string Arrival = "arrival";
    public const string PatientName = "patientName";
    public const string DoctorName = "doctorName";

    public static readonly IReadOnlySet<string> SortFields =
        new HashSet<string>(StringComparer.Ordinal)
        {
            Stage, Arrival, PatientName, DoctorName
        };
}
