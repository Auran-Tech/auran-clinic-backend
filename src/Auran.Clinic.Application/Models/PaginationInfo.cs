namespace Auran.Clinic.Application.Models;

public class PaginationInfo
{
    public int TotalCount { get; set; }

    public int RowCount { get; set; }

    public int CurrentPage { get; set; } = 1;

    public int TotalPage =>
        (int)Math.Ceiling((double)TotalCount / RowCount) < 1
            ? 1
            : (int)Math.Ceiling((double)TotalCount / RowCount);
}
