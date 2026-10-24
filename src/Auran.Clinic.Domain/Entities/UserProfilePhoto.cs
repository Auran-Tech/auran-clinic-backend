namespace Auran.Clinic.Domain.Entities;

public sealed class UserProfilePhoto : ClinicEntity
{
    public Guid UserId { get; set; }
    public required byte[] ImageData { get; set; }
    public required string ContentType { get; set; }
}
