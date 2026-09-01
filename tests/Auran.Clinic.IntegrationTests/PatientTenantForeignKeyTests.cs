using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DomainClinic = Auran.Clinic.Domain.Entities.Clinic;

namespace Auran.Clinic.IntegrationTests;

public sealed class PatientTenantForeignKeyTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task PatientCondition_DatabaseConstraint_RejectsCrossClinicPatient()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();

        var clinicA = await CreateClinicAsync(dbContext, "PC-A");
        var clinicB = await CreateClinicAsync(dbContext, "PC-B");
        var foreignPatient = await CreatePatientAsync(dbContext, clinicA.Id, "foreign-condition");
        var localPatient = await CreatePatientAsync(dbContext, clinicB.Id, "local-condition");
        var localUser = await CreateUserAsync(dbContext, userManager, clinicB.Id, "condition-recorder");

        var validRows = await InsertPatientConditionAsync(
            dbContext, clinicB.Id, localPatient.Id, localUser.Id, "Valid condition");
        Assert.Equal(1, validRows);

        var exception = await Assert.ThrowsAsync<SqlException>(() =>
            InsertPatientConditionAsync(
                dbContext, clinicB.Id, foreignPatient.Id, localUser.Id, "Cross-clinic condition"));

        Assert.Equal(547, exception.Number);
        Assert.Contains("FK_PatientConditions_Patients_PatientId_ClinicId", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FileRecord_DatabaseConstraint_RejectsCrossClinicUploader()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();

        var clinicA = await CreateClinicAsync(dbContext, "FILE-A");
        var clinicB = await CreateClinicAsync(dbContext, "FILE-B");
        var foreignUser = await CreateUserAsync(dbContext, userManager, clinicA.Id, "foreign-uploader");
        var localUser = await CreateUserAsync(dbContext, userManager, clinicB.Id, "local-uploader");

        var validRows = await InsertFileAsync(dbContext, clinicB.Id, localUser.Id, "valid-file");
        Assert.Equal(1, validRows);

        var exception = await Assert.ThrowsAsync<SqlException>(() =>
            InsertFileAsync(dbContext, clinicB.Id, foreignUser.Id, "cross-clinic-file"));

        Assert.Equal(547, exception.Number);
        Assert.Contains("FK_Files_Users_UploadedByUserId_ClinicId", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PatientProfileField_DatabaseConstraint_RejectsCrossClinicSection()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();

        var clinicA = await CreateClinicAsync(dbContext, "SECTION-A");
        var clinicB = await CreateClinicAsync(dbContext, "SECTION-B");
        var foreignSection = await CreateProfileSectionAsync(dbContext, clinicA.Id, "Foreign section");
        var localSection = await CreateProfileSectionAsync(dbContext, clinicB.Id, "Local section");

        var validRows = await InsertProfileFieldAsync(dbContext, clinicB.Id, localSection.Id, "Valid field");
        Assert.Equal(1, validRows);

        var exception = await Assert.ThrowsAsync<SqlException>(() =>
            InsertProfileFieldAsync(dbContext, clinicB.Id, foreignSection.Id, "Cross-clinic field"));

        Assert.Equal(547, exception.Number);
        Assert.Contains(
            "FK_PatientProfileFields_PatientProfileSections_SectionId_ClinicId",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task PatientProfileFieldOption_DatabaseConstraint_RejectsCrossClinicField()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();

        var clinicA = await CreateClinicAsync(dbContext, "FIELD-A");
        var clinicB = await CreateClinicAsync(dbContext, "FIELD-B");
        var sectionA = await CreateProfileSectionAsync(dbContext, clinicA.Id, "Section A");
        var sectionB = await CreateProfileSectionAsync(dbContext, clinicB.Id, "Section B");
        var foreignField = await CreateProfileFieldAsync(dbContext, clinicA.Id, sectionA.Id, "Foreign field");
        var localField = await CreateProfileFieldAsync(dbContext, clinicB.Id, sectionB.Id, "Local field");

        var validRows = await InsertProfileFieldOptionAsync(dbContext, clinicB.Id, localField.Id, "valid-option");
        Assert.Equal(1, validRows);

        var exception = await Assert.ThrowsAsync<SqlException>(() =>
            InsertProfileFieldOptionAsync(dbContext, clinicB.Id, foreignField.Id, "cross-clinic-option"));

        Assert.Equal(547, exception.Number);
        Assert.Contains(
            "FK_PatientProfileFieldOptions_PatientProfileFields_FieldId_ClinicId",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task PatientAttachment_DatabaseConstraint_RejectsCrossClinicFile()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();

        var clinicA = await CreateClinicAsync(dbContext, "ATTACH-A");
        var clinicB = await CreateClinicAsync(dbContext, "ATTACH-B");
        var userA = await CreateUserAsync(dbContext, userManager, clinicA.Id, "attachment-uploader-a");
        var userB = await CreateUserAsync(dbContext, userManager, clinicB.Id, "attachment-uploader-b");
        var foreignFile = await CreateFileAsync(dbContext, clinicA.Id, userA.Id, "Foreign attachment file");
        var localFile = await CreateFileAsync(dbContext, clinicB.Id, userB.Id, "Local attachment file");
        var localPatient = await CreatePatientAsync(dbContext, clinicB.Id, "attachment-patient");

        var validRows = await InsertPatientAttachmentAsync(
            dbContext, clinicB.Id, localPatient.Id, localFile.Id, "valid-attachment");
        Assert.Equal(1, validRows);

        var exception = await Assert.ThrowsAsync<SqlException>(() =>
            InsertPatientAttachmentAsync(
                dbContext, clinicB.Id, localPatient.Id, foreignFile.Id, "cross-clinic-attachment"));

        Assert.Equal(547, exception.Number);
        Assert.Contains("FK_PatientAttachments_Files_FileId_ClinicId", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PatientProfileValue_OptionalFile_AllowsNullAndRejectsCrossClinicFile()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();

        var clinicA = await CreateClinicAsync(dbContext, "VALUE-A");
        var clinicB = await CreateClinicAsync(dbContext, "VALUE-B");
        var userA = await CreateUserAsync(dbContext, userManager, clinicA.Id, "value-uploader-a");
        var foreignFile = await CreateFileAsync(dbContext, clinicA.Id, userA.Id, "Foreign value file");
        var localPatient = await CreatePatientAsync(dbContext, clinicB.Id, "value-patient");
        var localSection = await CreateProfileSectionAsync(dbContext, clinicB.Id, "Value section");
        var localField = await CreateProfileFieldAsync(dbContext, clinicB.Id, localSection.Id, "Value field");

        var nullFileRows = await InsertProfileValueAsync(
            dbContext, clinicB.Id, localPatient.Id, localField.Id, fileId: null, "plain text");
        Assert.Equal(1, nullFileRows);

        var exception = await Assert.ThrowsAsync<SqlException>(() =>
            InsertProfileValueAsync(
                dbContext, clinicB.Id, localPatient.Id, localField.Id, foreignFile.Id, "invalid file"));

        Assert.Equal(547, exception.Number);
        Assert.Contains("FK_PatientProfileValues_Files_FileId_ClinicId", exception.Message, StringComparison.Ordinal);
    }

    private static Task<int> InsertPatientConditionAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        Guid patientId,
        Guid recordedByUserId,
        string name)
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO [PatientConditions]
                ([Id], [ClinicId], [PatientId], [Name], [RecordedAtUtc], [RecordedByUserId], [CreatedDate])
            VALUES ({id}, {clinicId}, {patientId}, {name}, {now}, {recordedByUserId}, {now})
            """);
    }

    private static Task<int> InsertFileAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        Guid uploadedByUserId,
        string label)
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var storedName = $"{id:N}.bin";
        var storageKey = $"tenant-tests/{id:N}";
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO [Files]
                ([Id], [ClinicId], [OriginalName], [StoredName], [ContentType], [Size], [StorageProvider], [StorageKey], [UploadedAtUtc], [UploadedByUserId], [CreatedDate])
            VALUES ({id}, {clinicId}, {label}, {storedName}, {"application/octet-stream"}, {1L}, {"test"}, {storageKey}, {now}, {uploadedByUserId}, {now})
            """);
    }

    private static Task<int> InsertProfileFieldAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        Guid sectionId,
        string label)
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO [PatientProfileFields]
                ([Id], [ClinicId], [SectionId], [Label], [FieldType], [IsRequired], [IsEnabled], [SortOrder], [CreatedDate])
            VALUES ({id}, {clinicId}, {sectionId}, {label}, {nameof(DynamicFieldType.Text)}, {false}, {true}, {0}, {now})
            """);
    }

    private static Task<int> InsertProfileFieldOptionAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        Guid fieldId,
        string value)
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO [PatientProfileFieldOptions]
                ([Id], [ClinicId], [FieldId], [Label], [Value], [SortOrder], [CreatedDate])
            VALUES ({id}, {clinicId}, {fieldId}, {value}, {value}, {0}, {now})
            """);
    }

    private static Task<int> InsertPatientAttachmentAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        Guid patientId,
        Guid fileId,
        string category)
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO [PatientAttachments]
                ([Id], [ClinicId], [PatientId], [FileId], [Category], [CreatedDate])
            VALUES ({id}, {clinicId}, {patientId}, {fileId}, {category}, {now})
            """);
    }

    private static Task<int> InsertProfileValueAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        Guid patientId,
        Guid fieldId,
        Guid? fileId,
        string textValue)
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO [PatientProfileValues]
                ([Id], [ClinicId], [PatientId], [FieldId], [FileId], [TextValue], [CreatedDate])
            VALUES ({id}, {clinicId}, {patientId}, {fieldId}, {fileId}, {textValue}, {now})
            """);
    }

    private static async Task<DomainClinic> CreateClinicAsync(AuranClinicDbContext dbContext, string label)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var clinic = new DomainClinic
        {
            Id = Guid.NewGuid(),
            Name = $"Patient Tenant {label} {suffix}",
            Code = $"PT-{label}-{suffix}",
            CreatedDate = DateTime.UtcNow
        };
        dbContext.Clinics.Add(clinic);
        await dbContext.SaveChangesAsync();
        return clinic;
    }

    private static async Task<User> CreateUserAsync(
        AuranClinicDbContext dbContext,
        UserManager<ApplicationIdentityUser> userManager,
        Guid clinicId,
        string label)
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var email = $"{label}-{suffix}@auran.local";
        var identityUser = new ApplicationIdentityUser
        {
            UserName = email,
            Email = email,
            LockoutEnabled = true
        };
        var identityResult = await userManager.CreateAsync(identityUser, "ValidPassword1");
        Assert.True(
            identityResult.Succeeded,
            string.Join(", ", identityResult.Errors.Select(error => error.Description)));

        var user = new User
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            IdentityUserId = identityUser.Id,
            FullName = $"Patient Tenant {label}",
            Email = email,
            CreatedDate = DateTime.UtcNow
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    private static async Task<Patient> CreatePatientAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        string label)
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            PatientNumber = $"PT-{suffix}",
            FullName = $"Patient Tenant {label}",
            Phone = $"+20{Math.Abs(Guid.NewGuid().GetHashCode()):D10}"[..13],
            CreatedDate = DateTime.UtcNow
        };
        dbContext.Patients.Add(patient);
        await dbContext.SaveChangesAsync();
        return patient;
    }

    private static async Task<PatientProfileSection> CreateProfileSectionAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        string name)
    {
        var section = new PatientProfileSection
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            Name = name,
            IsEnabled = true,
            CreatedDate = DateTime.UtcNow
        };
        dbContext.PatientProfileSections.Add(section);
        await dbContext.SaveChangesAsync();
        return section;
    }

    private static async Task<PatientProfileField> CreateProfileFieldAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        Guid sectionId,
        string label)
    {
        var field = new PatientProfileField
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            SectionId = sectionId,
            Label = label,
            FieldType = DynamicFieldType.Text,
            IsEnabled = true,
            CreatedDate = DateTime.UtcNow
        };
        dbContext.PatientProfileFields.Add(field);
        await dbContext.SaveChangesAsync();
        return field;
    }

    private static async Task<FileRecord> CreateFileAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        Guid uploadedByUserId,
        string name)
    {
        var id = Guid.NewGuid();
        var file = new FileRecord
        {
            Id = id,
            ClinicId = clinicId,
            OriginalName = name,
            StoredName = $"{id:N}.bin",
            ContentType = "application/octet-stream",
            Size = 1,
            StorageProvider = "test",
            StorageKey = $"tenant-tests/{id:N}",
            UploadedAtUtc = DateTime.UtcNow,
            UploadedByUserId = uploadedByUserId,
            CreatedDate = DateTime.UtcNow
        };
        dbContext.Files.Add(file);
        await dbContext.SaveChangesAsync();
        return file;
    }
}
