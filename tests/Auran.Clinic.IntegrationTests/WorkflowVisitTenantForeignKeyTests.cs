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

public sealed class WorkflowVisitTenantForeignKeyTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task WorkflowTransition_DatabaseConstraint_RejectsCrossClinicStatus()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();

        var clinicA = await CreateClinicAsync(dbContext, "WF-A");
        var clinicB = await CreateClinicAsync(dbContext, "WF-B");
        var foreignStatus = await CreateWorkflowStatusAsync(dbContext, clinicA.Id, "FOREIGN");
        var localFrom = await CreateWorkflowStatusAsync(dbContext, clinicB.Id, "LOCAL-FROM");
        var localTo = await CreateWorkflowStatusAsync(dbContext, clinicB.Id, "LOCAL-TO");

        var validRows = await InsertWorkflowTransitionAsync(dbContext, clinicB.Id, localFrom.Id, localTo.Id);
        Assert.Equal(1, validRows);

        var exception = await Assert.ThrowsAsync<SqlException>(() =>
            InsertWorkflowTransitionAsync(dbContext, clinicB.Id, localFrom.Id, foreignStatus.Id));

        Assert.Equal(547, exception.Number);
        Assert.Contains(
            "FK_WorkflowTransitions_WorkflowStatuses_ToStatusId_ClinicId",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task QueueEntry_OptionalDoctor_AllowsNullAndRejectsCrossClinicDoctor()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();

        var clinicA = await CreateClinicAsync(dbContext, "QUEUE-A");
        var clinicB = await CreateClinicAsync(dbContext, "QUEUE-B");
        var foreignDoctor = await CreateUserAsync(dbContext, userManager, clinicA.Id, "foreign-queue-doctor");
        var localDoctor = await CreateUserAsync(dbContext, userManager, clinicB.Id, "local-queue-doctor");
        var localPatient = await CreatePatientAsync(dbContext, clinicB.Id, "queue-patient");
        var localVisit = await CreateVisitAsync(dbContext, clinicB.Id, localPatient.Id, localDoctor.Id);
        var localStatus = await CreateWorkflowStatusAsync(dbContext, clinicB.Id, "WAITING");

        var nullDoctorRows = await InsertQueueEntryAsync(
            dbContext, clinicB.Id, localPatient.Id, localVisit.Id, doctorId: null, localStatus.Id);
        Assert.Equal(1, nullDoctorRows);

        var exception = await Assert.ThrowsAsync<SqlException>(() =>
            InsertQueueEntryAsync(
                dbContext, clinicB.Id, localPatient.Id, localVisit.Id, foreignDoctor.Id, localStatus.Id));

        Assert.Equal(547, exception.Number);
        Assert.Contains("FK_QueueEntries_Users_DoctorId_ClinicId", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task QueueStatusHistory_OptionalFromStatus_AllowsNullAndRejectsCrossClinicStatus()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();

        var clinicA = await CreateClinicAsync(dbContext, "HISTORY-A");
        var clinicB = await CreateClinicAsync(dbContext, "HISTORY-B");
        var foreignFromStatus = await CreateWorkflowStatusAsync(dbContext, clinicA.Id, "FOREIGN-FROM");
        var localStatus = await CreateWorkflowStatusAsync(dbContext, clinicB.Id, "LOCAL-TO");
        var localUser = await CreateUserAsync(dbContext, userManager, clinicB.Id, "history-user");
        var localPatient = await CreatePatientAsync(dbContext, clinicB.Id, "history-patient");
        var localVisit = await CreateVisitAsync(dbContext, clinicB.Id, localPatient.Id, localUser.Id);
        var localQueue = await CreateQueueEntryAsync(
            dbContext, clinicB.Id, localPatient.Id, localVisit.Id, localUser.Id, localStatus.Id);

        var nullFromRows = await InsertQueueHistoryAsync(
            dbContext, clinicB.Id, localQueue.Id, fromStatusId: null, localStatus.Id, localUser.Id);
        Assert.Equal(1, nullFromRows);

        var exception = await Assert.ThrowsAsync<SqlException>(() =>
            InsertQueueHistoryAsync(
                dbContext, clinicB.Id, localQueue.Id, foreignFromStatus.Id, localStatus.Id, localUser.Id));

        Assert.Equal(547, exception.Number);
        Assert.Contains(
            "FK_QueueStatusHistory_WorkflowStatuses_FromStatusId_ClinicId",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task VisitSession_DatabaseConstraint_RejectsCrossClinicVisit()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();

        var clinicA = await CreateClinicAsync(dbContext, "SESSION-A");
        var clinicB = await CreateClinicAsync(dbContext, "SESSION-B");
        var doctorA = await CreateUserAsync(dbContext, userManager, clinicA.Id, "session-doctor-a");
        var doctorB = await CreateUserAsync(dbContext, userManager, clinicB.Id, "session-doctor-b");
        var patientA = await CreatePatientAsync(dbContext, clinicA.Id, "session-patient-a");
        var patientB = await CreatePatientAsync(dbContext, clinicB.Id, "session-patient-b");
        var foreignVisit = await CreateVisitAsync(dbContext, clinicA.Id, patientA.Id, doctorA.Id);
        var localVisit = await CreateVisitAsync(dbContext, clinicB.Id, patientB.Id, doctorB.Id);

        var validRows = await InsertVisitSessionAsync(dbContext, clinicB.Id, localVisit.Id, doctorB.Id, doctorB.Id);
        Assert.Equal(1, validRows);

        var exception = await Assert.ThrowsAsync<SqlException>(() =>
            InsertVisitSessionAsync(dbContext, clinicB.Id, foreignVisit.Id, doctorB.Id, doctorB.Id));

        Assert.Equal(547, exception.Number);
        Assert.Contains("FK_VisitSessions_Visits_VisitId_ClinicId", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ClinicalFieldOption_DatabaseConstraint_RejectsCrossClinicField()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();

        var clinicA = await CreateClinicAsync(dbContext, "CF-A");
        var clinicB = await CreateClinicAsync(dbContext, "CF-B");
        var foreignField = await CreateClinicalFieldAsync(dbContext, clinicA.Id, "Foreign field");
        var localField = await CreateClinicalFieldAsync(dbContext, clinicB.Id, "Local field");

        var validRows = await InsertClinicalFieldOptionAsync(dbContext, clinicB.Id, localField.Id, "valid");
        Assert.Equal(1, validRows);

        var exception = await Assert.ThrowsAsync<SqlException>(() =>
            InsertClinicalFieldOptionAsync(dbContext, clinicB.Id, foreignField.Id, "cross-clinic"));

        Assert.Equal(547, exception.Number);
        Assert.Contains(
            "FK_ClinicalFieldOptions_ClinicalFields_ClinicalFieldId_ClinicId",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task ClinicalMeasurement_OptionalVisit_AllowsNullAndRejectsCrossClinicVisit()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();

        var clinicA = await CreateClinicAsync(dbContext, "MEASURE-A");
        var clinicB = await CreateClinicAsync(dbContext, "MEASURE-B");
        var doctorA = await CreateUserAsync(dbContext, userManager, clinicA.Id, "measure-doctor-a");
        var doctorB = await CreateUserAsync(dbContext, userManager, clinicB.Id, "measure-doctor-b");
        var patientA = await CreatePatientAsync(dbContext, clinicA.Id, "measure-patient-a");
        var patientB = await CreatePatientAsync(dbContext, clinicB.Id, "measure-patient-b");
        var foreignVisit = await CreateVisitAsync(dbContext, clinicA.Id, patientA.Id, doctorA.Id);
        var localField = await CreateClinicalFieldAsync(dbContext, clinicB.Id, "Measurement field");

        var nullVisitRows = await InsertClinicalMeasurementAsync(
            dbContext, clinicB.Id, patientB.Id, visitId: null, localField.Id, doctorB.Id);
        Assert.Equal(1, nullVisitRows);

        var exception = await Assert.ThrowsAsync<SqlException>(() =>
            InsertClinicalMeasurementAsync(
                dbContext, clinicB.Id, patientB.Id, foreignVisit.Id, localField.Id, doctorB.Id));

        Assert.Equal(547, exception.Number);
        Assert.Contains("FK_ClinicalMeasurements_Visits_VisitId_ClinicId", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ClinicalOrder_DatabaseConstraint_RejectsCrossClinicCreator()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();

        var clinicA = await CreateClinicAsync(dbContext, "ORDER-A");
        var clinicB = await CreateClinicAsync(dbContext, "ORDER-B");
        var foreignCreator = await CreateUserAsync(dbContext, userManager, clinicA.Id, "foreign-order-creator");
        var localDoctor = await CreateUserAsync(dbContext, userManager, clinicB.Id, "local-order-doctor");
        var localPatient = await CreatePatientAsync(dbContext, clinicB.Id, "order-patient");
        var localVisit = await CreateVisitAsync(dbContext, clinicB.Id, localPatient.Id, localDoctor.Id);

        var validRows = await InsertClinicalOrderAsync(
            dbContext, clinicB.Id, localVisit.Id, localPatient.Id, localDoctor.Id, localDoctor.Id);
        Assert.Equal(1, validRows);

        var exception = await Assert.ThrowsAsync<SqlException>(() =>
            InsertClinicalOrderAsync(
                dbContext, clinicB.Id, localVisit.Id, localPatient.Id, localDoctor.Id, foreignCreator.Id));

        Assert.Equal(547, exception.Number);
        Assert.Contains("FK_ClinicalOrders_Users_CreatedByUserId_ClinicId", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ClinicalOrderSection_DatabaseConstraint_RejectsCrossClinicDefinition()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();

        var clinicA = await CreateClinicAsync(dbContext, "SECTION-A");
        var clinicB = await CreateClinicAsync(dbContext, "SECTION-B");
        var foreignDefinition = await CreateOrderSectionDefinitionAsync(dbContext, clinicA.Id, "Foreign definition");
        var localDefinition = await CreateOrderSectionDefinitionAsync(dbContext, clinicB.Id, "Local definition");
        var localDoctor = await CreateUserAsync(dbContext, userManager, clinicB.Id, "section-doctor");
        var localPatient = await CreatePatientAsync(dbContext, clinicB.Id, "section-patient");
        var localVisit = await CreateVisitAsync(dbContext, clinicB.Id, localPatient.Id, localDoctor.Id);
        var localOrder = await CreateClinicalOrderAsync(
            dbContext, clinicB.Id, localVisit.Id, localPatient.Id, localDoctor.Id, localDoctor.Id);

        var validRows = await InsertClinicalOrderSectionAsync(
            dbContext, clinicB.Id, localOrder.Id, localDefinition.Id);
        Assert.Equal(1, validRows);

        var exception = await Assert.ThrowsAsync<SqlException>(() =>
            InsertClinicalOrderSectionAsync(dbContext, clinicB.Id, localOrder.Id, foreignDefinition.Id));

        Assert.Equal(547, exception.Number);
        Assert.Contains(
            "FK_ClinicalOrderSections_ClinicalOrderSectionDefinitions_SectionDefinitionId_ClinicId",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task ClinicalOrderItem_DatabaseConstraint_RejectsCrossClinicSection()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();

        var clinicA = await CreateClinicAsync(dbContext, "ITEM-A");
        var clinicB = await CreateClinicAsync(dbContext, "ITEM-B");
        var doctorA = await CreateUserAsync(dbContext, userManager, clinicA.Id, "item-doctor-a");
        var doctorB = await CreateUserAsync(dbContext, userManager, clinicB.Id, "item-doctor-b");
        var patientA = await CreatePatientAsync(dbContext, clinicA.Id, "item-patient-a");
        var patientB = await CreatePatientAsync(dbContext, clinicB.Id, "item-patient-b");
        var visitA = await CreateVisitAsync(dbContext, clinicA.Id, patientA.Id, doctorA.Id);
        var visitB = await CreateVisitAsync(dbContext, clinicB.Id, patientB.Id, doctorB.Id);
        var definitionA = await CreateOrderSectionDefinitionAsync(dbContext, clinicA.Id, "Definition A");
        var definitionB = await CreateOrderSectionDefinitionAsync(dbContext, clinicB.Id, "Definition B");
        var orderA = await CreateClinicalOrderAsync(dbContext, clinicA.Id, visitA.Id, patientA.Id, doctorA.Id, doctorA.Id);
        var orderB = await CreateClinicalOrderAsync(dbContext, clinicB.Id, visitB.Id, patientB.Id, doctorB.Id, doctorB.Id);
        var foreignSection = await CreateClinicalOrderSectionAsync(dbContext, clinicA.Id, orderA.Id, definitionA.Id);
        var localSection = await CreateClinicalOrderSectionAsync(dbContext, clinicB.Id, orderB.Id, definitionB.Id);

        var validRows = await InsertClinicalOrderItemAsync(dbContext, clinicB.Id, localSection.Id, "Valid item");
        Assert.Equal(1, validRows);

        var exception = await Assert.ThrowsAsync<SqlException>(() =>
            InsertClinicalOrderItemAsync(dbContext, clinicB.Id, foreignSection.Id, "Cross-clinic item"));

        Assert.Equal(547, exception.Number);
        Assert.Contains(
            "FK_ClinicalOrderItems_ClinicalOrderSections_ClinicalOrderSectionId_ClinicId",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task ClinicalOrderAttachment_OptionalSection_AllowsNullAndRejectsCrossClinicFile()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationIdentityUser>>();

        var clinicA = await CreateClinicAsync(dbContext, "ATTACH-A");
        var clinicB = await CreateClinicAsync(dbContext, "ATTACH-B");
        var userA = await CreateUserAsync(dbContext, userManager, clinicA.Id, "attachment-user-a");
        var userB = await CreateUserAsync(dbContext, userManager, clinicB.Id, "attachment-user-b");
        var patientB = await CreatePatientAsync(dbContext, clinicB.Id, "attachment-patient-b");
        var visitB = await CreateVisitAsync(dbContext, clinicB.Id, patientB.Id, userB.Id);
        var orderB = await CreateClinicalOrderAsync(dbContext, clinicB.Id, visitB.Id, patientB.Id, userB.Id, userB.Id);
        var foreignFile = await CreateFileAsync(dbContext, clinicA.Id, userA.Id, "Foreign order attachment");
        var localFile = await CreateFileAsync(dbContext, clinicB.Id, userB.Id, "Local order attachment");

        var nullSectionRows = await InsertClinicalOrderAttachmentAsync(
            dbContext, clinicB.Id, orderB.Id, sectionId: null, localFile.Id);
        Assert.Equal(1, nullSectionRows);

        var exception = await Assert.ThrowsAsync<SqlException>(() =>
            InsertClinicalOrderAttachmentAsync(
                dbContext, clinicB.Id, orderB.Id, sectionId: null, foreignFile.Id));

        Assert.Equal(547, exception.Number);
        Assert.Contains("FK_ClinicalOrderAttachments_Files_FileId_ClinicId", exception.Message, StringComparison.Ordinal);
    }

    private static Task<int> InsertWorkflowTransitionAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        Guid fromStatusId,
        Guid toStatusId)
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO [WorkflowTransitions]
                ([Id], [ClinicId], [FromStatusId], [ToStatusId], [CreatedDate])
            VALUES ({id}, {clinicId}, {fromStatusId}, {toStatusId}, {now})
            """);
    }

    private static Task<int> InsertQueueEntryAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        Guid patientId,
        Guid visitId,
        Guid? doctorId,
        Guid workflowStatusId)
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO [QueueEntries]
                ([Id], [ClinicId], [PatientId], [VisitId], [DoctorId], [WorkflowStatusId], [EntryAtUtc], [CreatedDate])
            VALUES ({id}, {clinicId}, {patientId}, {visitId}, {doctorId}, {workflowStatusId}, {now}, {now})
            """);
    }

    private static Task<int> InsertQueueHistoryAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        Guid queueEntryId,
        Guid? fromStatusId,
        Guid toStatusId,
        Guid changedByUserId)
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO [QueueStatusHistory]
                ([Id], [ClinicId], [QueueEntryId], [FromStatusId], [ToStatusId], [ChangedAtUtc], [ChangedByUserId], [CreatedDate])
            VALUES ({id}, {clinicId}, {queueEntryId}, {fromStatusId}, {toStatusId}, {now}, {changedByUserId}, {now})
            """);
    }

    private static Task<int> InsertVisitSessionAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        Guid visitId,
        Guid doctorId,
        Guid createdByUserId)
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO [VisitSessions]
                ([Id], [ClinicId], [VisitId], [DoctorId], [StartedAtUtc], [CreatedByUserId], [CreatedDate])
            VALUES ({id}, {clinicId}, {visitId}, {doctorId}, {now}, {createdByUserId}, {now})
            """);
    }

    private static Task<int> InsertClinicalFieldOptionAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        Guid clinicalFieldId,
        string value)
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO [ClinicalFieldOptions]
                ([Id], [ClinicId], [ClinicalFieldId], [Label], [Value], [SortOrder], [CreatedDate])
            VALUES ({id}, {clinicId}, {clinicalFieldId}, {value}, {value}, {0}, {now})
            """);
    }

    private static Task<int> InsertClinicalMeasurementAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        Guid patientId,
        Guid? visitId,
        Guid clinicalFieldId,
        Guid recordedByUserId)
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO [ClinicalMeasurements]
                ([Id], [ClinicId], [PatientId], [VisitId], [ClinicalFieldId], [NumberValue], [RecordedAtUtc], [RecordedByUserId], [CreatedDate])
            VALUES ({id}, {clinicId}, {patientId}, {visitId}, {clinicalFieldId}, {1m}, {now}, {recordedByUserId}, {now})
            """);
    }

    private static Task<int> InsertClinicalOrderAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        Guid visitId,
        Guid patientId,
        Guid doctorId,
        Guid createdByUserId)
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO [ClinicalOrders]
                ([Id], [ClinicId], [VisitId], [PatientId], [DoctorId], [CreatedByUserId], [CreatedDate])
            VALUES ({id}, {clinicId}, {visitId}, {patientId}, {doctorId}, {createdByUserId}, {now})
            """);
    }

    private static Task<int> InsertClinicalOrderSectionAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        Guid clinicalOrderId,
        Guid sectionDefinitionId)
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO [ClinicalOrderSections]
                ([Id], [ClinicId], [ClinicalOrderId], [SectionDefinitionId], [SortOrder], [CreatedDate])
            VALUES ({id}, {clinicId}, {clinicalOrderId}, {sectionDefinitionId}, {0}, {now})
            """);
    }

    private static Task<int> InsertClinicalOrderItemAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        Guid clinicalOrderSectionId,
        string name)
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO [ClinicalOrderItems]
                ([Id], [ClinicId], [ClinicalOrderSectionId], [Name], [CreatedDate])
            VALUES ({id}, {clinicId}, {clinicalOrderSectionId}, {name}, {now})
            """);
    }

    private static Task<int> InsertClinicalOrderAttachmentAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        Guid clinicalOrderId,
        Guid? sectionId,
        Guid fileId)
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO [ClinicalOrderAttachments]
                ([Id], [ClinicId], [ClinicalOrderId], [ClinicalOrderSectionId], [FileId], [CreatedDate])
            VALUES ({id}, {clinicId}, {clinicalOrderId}, {sectionId}, {fileId}, {now})
            """);
    }

    private static async Task<DomainClinic> CreateClinicAsync(AuranClinicDbContext dbContext, string label)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var clinic = new DomainClinic
        {
            Id = Guid.NewGuid(),
            Name = $"Workflow Tenant {label} {suffix}",
            Code = $"WV-{label}-{suffix}",
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
            FullName = $"Workflow Tenant {label}",
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
            PatientNumber = $"WV-{suffix}",
            FullName = $"Workflow Tenant {label}",
            Phone = $"+20{Math.Abs(Guid.NewGuid().GetHashCode()):D10}"[..13],
            CreatedDate = DateTime.UtcNow
        };
        dbContext.Patients.Add(patient);
        await dbContext.SaveChangesAsync();
        return patient;
    }

    private static async Task<WorkflowStatus> CreateWorkflowStatusAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        string code)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var status = new WorkflowStatus
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            Code = $"{code}-{suffix}",
            Name = $"Status {code} {suffix}",
            Color = "#000000",
            CreatedDate = DateTime.UtcNow
        };
        dbContext.WorkflowStatuses.Add(status);
        await dbContext.SaveChangesAsync();
        return status;
    }

    private static async Task<Visit> CreateVisitAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        Guid patientId,
        Guid doctorId)
    {
        var visit = new Visit
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            PatientId = patientId,
            DoctorId = doctorId,
            Status = VisitStatus.Open,
            DocumentationStatus = DocumentationStatus.NotStarted,
            EntryAtUtc = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };
        dbContext.Visits.Add(visit);
        await dbContext.SaveChangesAsync();
        return visit;
    }

    private static async Task<QueueEntry> CreateQueueEntryAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        Guid patientId,
        Guid visitId,
        Guid doctorId,
        Guid workflowStatusId)
    {
        var queue = new QueueEntry
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            PatientId = patientId,
            VisitId = visitId,
            DoctorId = doctorId,
            WorkflowStatusId = workflowStatusId,
            EntryAtUtc = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };
        dbContext.QueueEntries.Add(queue);
        await dbContext.SaveChangesAsync();
        return queue;
    }

    private static async Task<ClinicalField> CreateClinicalFieldAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        string name)
    {
        var field = new ClinicalField
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            Name = name,
            FieldType = DynamicFieldType.Number,
            IsEnabled = true,
            CreatedDate = DateTime.UtcNow
        };
        dbContext.ClinicalFields.Add(field);
        await dbContext.SaveChangesAsync();
        return field;
    }

    private static async Task<ClinicalOrderSectionDefinition> CreateOrderSectionDefinitionAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        string name)
    {
        var definition = new ClinicalOrderSectionDefinition
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            Name = name,
            SectionType = ClinicalOrderSectionType.Structured,
            IsEnabled = true,
            CreatedDate = DateTime.UtcNow
        };
        dbContext.ClinicalOrderSectionDefinitions.Add(definition);
        await dbContext.SaveChangesAsync();
        return definition;
    }

    private static async Task<ClinicalOrder> CreateClinicalOrderAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        Guid visitId,
        Guid patientId,
        Guid doctorId,
        Guid createdByUserId)
    {
        var order = new ClinicalOrder
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            VisitId = visitId,
            PatientId = patientId,
            DoctorId = doctorId,
            CreatedByUserId = createdByUserId,
            CreatedDate = DateTime.UtcNow
        };
        dbContext.ClinicalOrders.Add(order);
        await dbContext.SaveChangesAsync();
        return order;
    }

    private static async Task<ClinicalOrderSection> CreateClinicalOrderSectionAsync(
        AuranClinicDbContext dbContext,
        Guid clinicId,
        Guid orderId,
        Guid definitionId)
    {
        var section = new ClinicalOrderSection
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            ClinicalOrderId = orderId,
            SectionDefinitionId = definitionId,
            CreatedDate = DateTime.UtcNow
        };
        dbContext.ClinicalOrderSections.Add(section);
        await dbContext.SaveChangesAsync();
        return section;
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
            StorageKey = $"workflow-tenant-tests/{id:N}",
            UploadedAtUtc = DateTime.UtcNow,
            UploadedByUserId = uploadedByUserId,
            CreatedDate = DateTime.UtcNow
        };
        dbContext.Files.Add(file);
        await dbContext.SaveChangesAsync();
        return file;
    }
}
