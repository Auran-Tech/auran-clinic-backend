using Auran.Clinic.Api.Contracts.Auditing;
using Auran.Clinic.Api.Contracts.Authentication;
using Auran.Clinic.Api.Contracts.Clinics;
using Auran.Clinic.Api.Contracts.Features;
using Auran.Clinic.Api.Contracts.Files;
using AppAudit = Auran.Clinic.Application.Auditing;
using AppAuth = Auran.Clinic.Application.Authentication;
using AppClinics = Auran.Clinic.Application.Clinics;
using AppFeatures = Auran.Clinic.Application.Features;
using AppFiles = Auran.Clinic.Application.Files;

namespace Auran.Clinic.Api.Mappings;

public static class ApiRequestMappings
{
    public static AppAuth.LoginRequest ToServiceRequest(this LoginApiRequest request) => new()
    {
        Email = request.Email!,
        Password = request.Password!
    };

    public static AppAuth.RefreshTokenRequest ToServiceRequest(this RefreshTokenApiRequest request) => new()
    {
        RefreshToken = request.RefreshToken!
    };

    public static AppClinics.CreateClinicRequest ToServiceRequest(this CreateClinicApiRequest request) => new()
    {
        Name = request.Name!,
        CodePrefix = request.CodePrefix!,
        LogoUrl = request.LogoUrl,
        PrimaryColor = request.PrimaryColor,
        SecondaryColor = request.SecondaryColor,
        FontFamily = request.FontFamily,
        WelcomeTitle = request.WelcomeTitle,
        WelcomeMessage = request.WelcomeMessage,
        WelcomeButtonText = request.WelcomeButtonText,
        TimeZoneId = request.TimeZoneId!,
        CountryCode = request.CountryCode!,
        CityCode = request.CityCode!,
        PatientNumberPrefix = request.PatientNumberPrefix!,
        Locale = request.Locale!,
        Phone = request.Phone!,
        Email = request.Email!,
        Address = request.Address!,
        Website = request.Website,
        Admin = request.Admin!.ToServiceRequest()
    };

    public static AppClinics.InitialAdminRequest ToServiceRequest(this InitialAdminApiRequest request) => new()
    {
        FullName = request.FullName!,
        Email = request.Email!,
        Phone = request.Phone,
        Password = request.Password!
    };

    public static AppClinics.UpdateClinicRequest ToServiceRequest(this UpdateClinicApiRequest request) => new()
    {
        Name = request.Name!,
        LogoUrl = request.LogoUrl,
        PrimaryColor = request.PrimaryColor,
        SecondaryColor = request.SecondaryColor,
        FontFamily = request.FontFamily,
        WelcomeTitle = request.WelcomeTitle,
        WelcomeMessage = request.WelcomeMessage,
        WelcomeButtonText = request.WelcomeButtonText,
        TimeZoneId = request.TimeZoneId!,
        CountryCode = request.CountryCode!,
        CityCode = request.CityCode!,
        PatientNumberPrefix = request.PatientNumberPrefix!,
        Locale = request.Locale!,
        Phone = request.Phone!,
        Email = request.Email!,
        Address = request.Address!,
        Website = request.Website
    };

    public static AppClinics.UpdateClinicSettingsRequest ToServiceRequest(this UpdateClinicSettingsApiRequest request) => new()
    {
        LogoUrl = request.LogoUrl,
        PrimaryColor = request.PrimaryColor,
        SecondaryColor = request.SecondaryColor,
        FontFamily = request.FontFamily,
        WelcomeTitle = request.WelcomeTitle,
        WelcomeMessage = request.WelcomeMessage,
        WelcomeButtonText = request.WelcomeButtonText,
        TimeZoneId = request.TimeZoneId,
        CountryCode = request.CountryCode,
        CityCode = request.CityCode,
        PatientNumberPrefix = request.PatientNumberPrefix,
        Phone = request.Phone,
        Email = request.Email,
        Address = request.Address,
        Website = request.Website,
        Locale = request.Locale,
        DateFormat = request.DateFormat,
        TimeFormat = request.TimeFormat,
        DocumentationReminderHours = request.DocumentationReminderHours!.Value,
        PrescriptionHeader = request.PrescriptionHeader,
        PrescriptionFooter = request.PrescriptionFooter
    };

    public static AppClinics.SetClinicStatusRequest ToServiceRequest(this SetClinicStatusApiRequest request) => new()
    {
        IsActive = request.IsActive!.Value
    };

    public static AppClinics.ClinicSearchRequest ToServiceRequest(this ClinicSearchApiRequest request) => new()
    {
        Search = request.Search,
        IsActive = request.IsActive,
        Page = request.Page,
        PageSize = request.PageSize
    };

    public static AppFeatures.UpdateClinicFeaturesRequest ToServiceRequest(this UpdateClinicFeaturesApiRequest request) => new()
    {
        Features = request.Features!.Select(ToServiceRequest).ToList()
    };

    public static AppFeatures.UpdateClinicFeatureItem ToServiceRequest(this UpdateClinicFeatureApiRequest request) => new()
    {
        Code = request.Code!,
        IsEnabled = request.IsEnabled,
        ConfigurationJson = request.ConfigurationJson
    };

    public static AppFiles.CreateFileUploadSessionRequest ToServiceRequest(this CreateFileUploadSessionApiRequest request) => new()
    {
        FileName = request.FileName!,
        ContentType = request.ContentType!,
        Size = request.Size!.Value
    };

    public static AppAudit.AuditLogSearchRequest ToServiceRequest(this AuditLogSearchApiRequest request) => new()
    {
        Scope = request.Scope,
        ClinicId = request.ClinicId,
        ActorType = request.ActorType,
        ActorId = request.ActorId,
        Action = request.Action,
        Category = request.Category,
        EntityType = request.EntityType,
        EntityId = request.EntityId,
        FromUtc = request.FromUtc,
        ToUtc = request.ToUtc,
        Page = request.Page,
        PageSize = request.PageSize
    };
}
