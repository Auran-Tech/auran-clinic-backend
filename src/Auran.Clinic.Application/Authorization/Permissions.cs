namespace Auran.Clinic.Application.Authorization;

public static class Permissions
{
    public static class Patients
    {
        public const string View = "Patients.View";
        public const string Create = "Patients.Create";
        public const string Update = "Patients.Update";
    }

    public static class Users
    {
        public const string View = "Users.View";
        public const string Manage = "Users.Manage";
    }

    public static class Roles
    {
        public const string View = "Roles.View";
        public const string Manage = "Roles.Manage";
    }

    public static class Settings
    {
        public const string View = "Settings.View";
        public const string Manage = "Settings.Manage";
    }
}
