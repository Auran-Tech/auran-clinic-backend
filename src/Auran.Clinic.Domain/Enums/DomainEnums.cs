namespace Auran.Clinic.Domain.Enums;

public enum DynamicFieldType
{
    Text = 1,
    LongText = 2,
    Number = 3,
    Boolean = 4,
    Date = 5,
    Image = 6,
    File = 7,
    SingleSelect = 8,
    MultiSelect = 9
}

public enum DocumentationStatus
{
    NotStarted = 1,
    Draft = 2,
    Pending = 3,
    Completed = 4
}

public enum VisitStatus
{
    Open = 1,
    Completed = 2,
    Cancelled = 3
}

public enum FollowUpStatus
{
    Open = 1,
    Completed = 2,
    Cancelled = 3
}

public enum ClinicalOrderSectionType
{
    Structured = 1,
    Text = 2,
    Image = 3,
    File = 4
}
