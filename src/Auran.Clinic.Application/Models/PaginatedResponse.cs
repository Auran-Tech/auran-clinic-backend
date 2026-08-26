namespace Auran.Clinic.Application.Models;

public class PaginatedResponse<T>
{
    public List<T> Data { get; set; } = new List<T>();

    public required PaginationInfo Setting { get; set; }
}
