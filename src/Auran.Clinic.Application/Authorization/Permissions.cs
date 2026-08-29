namespace Auran.Clinic.Application.Authorization;

public static class Permissions
{
    public static class Platform
    {
        public static class Clinics
        {
            public const string View = "Platform.Clinics.View";
            public const string Create = "Platform.Clinics.Create";
            public const string Update = "Platform.Clinics.Update";
            public const string SetStatus = "Platform.Clinics.SetStatus";
            public const string ManageFeatures = "Platform.Clinics.Features.Manage";
        }

        public static class AuditLogs
        {
            public const string View = "Platform.AuditLogs.View";
        }

        public static class Users
        {
            public const string Manage = "Platform.Users.Manage";
        }
    }

    public static class Clinic
    {
        public static class AuditLogs
        {
            public const string View = "AuditLogs.View";
        }

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

        public static class Queue
        {
            public const string View = "Queue.View";
            public const string Manage = "Queue.Manage";
        }

        public static class Visits
        {
            public const string View = "Visits.View";
            public const string Create = "Visits.Create";
            public const string Update = "Visits.Update";
        }

        public static class Clinical
        {
            public const string View = "Clinical.View";
            public const string Manage = "Clinical.Manage";
        }

        public static class FollowUps
        {
            public const string View = "FollowUps.View";
            public const string Manage = "FollowUps.Manage";
        }

        public static class Reports
        {
            public const string View = "Reports.View";
            public const string Export = "Reports.Export";
        }

        public static class Settings
        {
            public const string View = "Settings.View";
            public const string Manage = "Settings.Manage";
        }

        public static class Files
        {
            public const string View = "Files.View";
            public const string Upload = "Files.Upload";
        }
    }
}
