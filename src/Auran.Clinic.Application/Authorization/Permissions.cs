namespace Auran.Clinic.Application.Authorization;

public static class Permissions
{
    public static class Audit
    {
        public const string View = "Audit_View";
    }

    public static class Patients
    {
        public const string View = "Patient_View";
        public const string Create = "Patient_Create";
        public const string Update = "Patient_Edit_Basic";
    }

    public static class Users
    {
        public const string View = "Users_View";
        public const string Manage = "Users_Manage";
        public const string ManageStatus = "Users_Manage_Status";
    }

    public static class Roles
    {
        public const string View = "RBAC_View";
        public const string Manage = "RBAC_Manage";
    }

    public static class Queue
    {
        public const string View = "Queue_View";
        public const string Move = "Queue_Move";
    }

    public static class Visits
    {
        public const string View = "Visit_View";
        public const string Start = "Visit_Start";
        public const string Edit = "Visit_Edit";
    }

    public static class MedicalProfile
    {
        public const string View = "MedicalProfile_View";
        public const string Edit = "MedicalProfile_Edit";
    }

    public static class FollowUps
    {
        public const string View = "FollowUp_View";
        public const string Manage = "FollowUp_Manage";
    }

    public static class Reports
    {
        public const string View = "Reports_View";
        public const string Export = "Reports_Export";
    }

    public static class Settings
    {
        public const string View = "Settings_View";
        public const string Manage = "Settings_Manage";
    }

    public static class Files
    {
        public const string View = "Files_View";
        public const string Upload = "Files_Upload";
    }

    public static class Attendance
    {
        public const string CreateShift = "Attendance_Create_Shift";
    }
}
