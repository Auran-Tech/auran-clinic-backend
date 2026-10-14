using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Identity;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Auran.Clinic.Infrastructure.Files;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Auran.Clinic.IntegrationTests.ReleaseReadiness;

public sealed class AuthenticatedTenantBoundaryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthenticatedTenantBoundaryTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Valid_clinic_token_cannot_read_another_clinics_visit_history()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuranClinicDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var clinicA = Guid.NewGuid();
        var clinicB = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var patientA = Guid.NewGuid();
        var patientB = Guid.NewGuid();
        var identityId = Guid.NewGuid().ToString("N");

        db.Clinics.AddRange(
            new Auran.Clinic.Domain.Entities.Clinic { Id = clinicA, Name = "Tenant test A", Code = "T" + clinicA.ToString("N")[..12] },
            new Auran.Clinic.Domain.Entities.Clinic { Id = clinicB, Name = "Tenant test B", Code = "T" + clinicB.ToString("N")[..12] });
        db.Set<ApplicationIdentityUser>().Add(new ApplicationIdentityUser
        {
            Id = identityId,
            UserName = "test-" + identityId,
            NormalizedUserName = "TEST-" + identityId.ToUpperInvariant(),
            Email = identityId + "@example.invalid",
            NormalizedEmail = identityId.ToUpperInvariant() + "@EXAMPLE.INVALID",
            SecurityStamp = Guid.NewGuid().ToString()
        });
        db.Users.Add(new User
        {
            Id = userId, ClinicId = clinicA, IdentityUserId = identityId,
            FullName = "Tenant test actor", IsActive = true
        });
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = sessionId, ClinicId = clinicA, UserId = userId,
            TokenHash = Guid.NewGuid().ToString("N"),
            ExpiresDate = DateTime.UtcNow.AddHours(1)
        });
        db.Patients.AddRange(
            new Patient { Id = patientA, ClinicId = clinicA, PatientNumber = "A" + patientA.ToString("N")[..12], FullName = "A", Phone = patientA.ToString("N") },
            new Patient { Id = patientB, ClinicId = clinicB, PatientNumber = "B" + patientB.ToString("N")[..12], FullName = "B", Phone = patientB.ToString("N") });
        var doctorB = Guid.NewGuid();
        var identityB = Guid.NewGuid().ToString("N");
        db.Set<ApplicationIdentityUser>().Add(new ApplicationIdentityUser
        {
            Id = identityB, UserName = "test-" + identityB,
            NormalizedUserName = "TEST-" + identityB.ToUpperInvariant(),
            Email = identityB + "@example.invalid",
            NormalizedEmail = identityB.ToUpperInvariant() + "@EXAMPLE.INVALID",
            SecurityStamp = Guid.NewGuid().ToString()
        });
        db.Users.Add(new User { Id = doctorB, ClinicId = clinicB, IdentityUserId = identityB, FullName = "Clinic B doctor", IsActive = true });
        var visitA = Guid.NewGuid();
        var visitB = Guid.NewGuid();
        db.Visits.AddRange(
            new Visit { Id = visitA, ClinicId = clinicA, PatientId = patientA, DoctorId = userId, EntryAtUtc = DateTime.UtcNow },
            new Visit { Id = visitB, ClinicId = clinicB, PatientId = patientB, DoctorId = doctorB, EntryAtUtc = DateTime.UtcNow });
        var fileA = Guid.NewGuid();
        var fileB = Guid.NewGuid();
        var attachmentB = Guid.NewGuid();
        db.Files.AddRange(
            new FileRecord { Id = fileA, ClinicId = clinicA, OriginalName = "a.pdf", StoredName = "a.pdf", ContentType = "application/pdf", Size = 4, StorageProvider = "local", StorageKey = "fixture-a-" + fileA, UploadedAtUtc = DateTime.UtcNow, UploadedByUserId = userId },
            new FileRecord { Id = fileB, ClinicId = clinicB, OriginalName = "b.pdf", StoredName = "b.pdf", ContentType = "application/pdf", Size = 4, StorageProvider = "local", StorageKey = "fixture-b-" + fileB, UploadedAtUtc = DateTime.UtcNow, UploadedByUserId = doctorB });
        db.PatientAttachments.AddRange(
            new PatientAttachment { Id = Guid.NewGuid(), ClinicId = clinicA, PatientId = patientA, FileId = fileA },
            new PatientAttachment { Id = attachmentB, ClinicId = clinicB, PatientId = patientB, FileId = fileB });
        await db.SaveChangesAsync();

        using var client = _factory.CreateClient();
        var token = CreateToken(configuration, userId, clinicA, sessionId, "Visit_View");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var own = await client.GetAsync($"/api/visits/history?patientId={patientA}");
        var other = await client.GetAsync($"/api/visits/history?patientId={patientB}");

        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, other.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Patient_View"));
        var ownMedicalHistory = await client.GetAsync($"/api/patient-medical-history?patientId={patientA}");
        var otherMedicalHistory = await client.GetAsync($"/api/patient-medical-history?patientId={patientB}");
        Assert.Equal(HttpStatusCode.OK, ownMedicalHistory.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, otherMedicalHistory.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Visit_View"));
        var medicalHistoryDenied = await client.GetAsync($"/api/patient-medical-history?patientId={patientA}");
        Assert.Equal(HttpStatusCode.Forbidden, medicalHistoryDenied.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Visit_View"));
        var ownMeasurements = await client.GetAsync($"/api/clinical-measurements?visitId={visitA}");
        var foreignMeasurements = await client.GetAsync($"/api/clinical-measurements?visitId={visitB}");
        Assert.Equal(HttpStatusCode.OK, ownMeasurements.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, foreignMeasurements.StatusCode);

        // The order attachment workspace exposes file names/notes and needs Files_View.
        var fileWorkspaceDenied = await client.GetAsync($"/api/clinical-order-attachments?visitId={visitA}");
        Assert.Equal(HttpStatusCode.Forbidden, fileWorkspaceDenied.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Visit_View", "Files_View"));
        var ownFileWorkspace = await client.GetAsync($"/api/clinical-order-attachments?visitId={visitA}");
        var foreignFileWorkspace = await client.GetAsync($"/api/clinical-order-attachments?visitId={visitB}");
        Assert.Equal(HttpStatusCode.OK, ownFileWorkspace.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, foreignFileWorkspace.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Patient_View"));
        foreach (var endpoint in new[] { "clinical-measurements", "clinical-order-attachments" })
        {
            var deniedResponse = await client.GetAsync($"/api/{endpoint}?visitId={visitA}");
            Assert.Equal(HttpStatusCode.Forbidden, deniedResponse.StatusCode);
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Files_View"));
        var ownAttachments = await client.GetAsync($"/api/patient-attachments?patientId={patientA}");
        Assert.Equal(HttpStatusCode.OK, ownAttachments.StatusCode);
        Assert.Contains(fileA.ToString(), await ownAttachments.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        var foreignAttachments = await client.GetAsync($"/api/patient-attachments?patientId={patientB}");
        Assert.Equal(HttpStatusCode.OK, foreignAttachments.StatusCode);
        var attachmentJson = await foreignAttachments.Content.ReadAsStringAsync();
        Assert.Contains("\"data\":[]", attachmentJson, StringComparison.OrdinalIgnoreCase);

        var foreignDownload = await client.GetAsync($"/api/patient-attachments/download?fileId={fileB}");
        Assert.Equal(HttpStatusCode.NotFound, foreignDownload.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Files_Upload"));
        var foreignDelete = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/patient-attachments")
        {
            Content = JsonContent.Create(new { attachmentId = attachmentB })
        });
        Assert.Equal(HttpStatusCode.NotFound, foreignDelete.StatusCode);

        using var filePayload = new MultipartFormDataContent();
        filePayload.Add(new StringContent(patientB.ToString()), "PatientId");
        using var fileContent = new ByteArrayContent(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        filePayload.Add(fileContent, "File", "test.pdf");
        var foreignUpload = await client.PostAsync("/api/patient-attachments", filePayload);
        Assert.Equal(HttpStatusCode.NotFound, foreignUpload.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Files_View"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Patient_View"));
        var fileReadDenied = await client.GetAsync($"/api/patient-attachments?patientId={patientA}");
        Assert.Equal(HttpStatusCode.Forbidden, fileReadDenied.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Visit_View"));
        foreach (var endpoint in new[] { "clinical-measurements", "clinical-order-attachments" })
        {
            var deniedWrite = await client.PostAsJsonAsync($"/api/{endpoint}", new { visitId = visitB });
            Assert.Equal(HttpStatusCode.Forbidden, deniedWrite.StatusCode);
        }

        // A valid editing permission must never override the clinic boundary.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Visit_Edit"));
        var crossTenantWrite = await client.PostAsJsonAsync(
            "/api/clinical-measurements",
            new { visitId = visitB, values = new[] { new { fieldId = Guid.NewGuid(), textValue = "test" } } });
        Assert.Equal(HttpStatusCode.NotFound, crossTenantWrite.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Visit_Edit", "Files_View"));
        var crossTenantOrderAttachmentWrite = await client.PostAsJsonAsync(
            "/api/clinical-order-attachments",
            new { visitId = visitB, fileId = Guid.NewGuid(), sectionDefinitionId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.NotFound, crossTenantOrderAttachmentWrite.StatusCode);

        // Historical clinical orders must not be rewritten after the visit exits.
        var closedVisitId = Guid.NewGuid();
        db.Visits.Add(new Visit
        {
            Id = closedVisitId, ClinicId = clinicA, PatientId = patientA,
            DoctorId = userId, EntryAtUtc = DateTime.UtcNow,
            Status = Auran.Clinic.Domain.Enums.VisitStatus.Completed
        });
        await db.SaveChangesAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Visit_Edit"));
        using (var closedOrder = await client.PutAsJsonAsync(
            "/api/clinical-orders", new { visitId = closedVisitId, sections = Array.Empty<object>() }))
        {
            Assert.Equal(HttpStatusCode.Conflict, closedOrder.StatusCode);
            Assert.Contains("visit_closed", await closedOrder.Content.ReadAsStringAsync());
        }
        Assert.False(await db.ClinicalOrders.AsNoTracking()
            .AnyAsync(order => order.VisitId == closedVisitId));

        // A file still linked into a clinical order must remain reachable through
        // its owning patient attachment and cannot be deleted independently.
        var referencedPatientAttachmentId = Guid.NewGuid();
        db.PatientAttachments.Add(new PatientAttachment
        {
            Id = referencedPatientAttachmentId, ClinicId = clinicA,
            PatientId = patientA, FileId = fileA
        });
        await db.SaveChangesAsync();

        // Saving text/structured content must never drop preexisting File/Image
        // sections and the linked patient attachment.
        var orderId = Guid.NewGuid();
        var fileSectionId = Guid.NewGuid();
        var fileDefinitionId = Guid.NewGuid();
        var fileLinkId = Guid.NewGuid();
        db.ClinicalOrderSectionDefinitions.Add(new ClinicalOrderSectionDefinition
        {
            Id = fileDefinitionId, ClinicId = clinicA, Name = "Attached studies",
            SectionType = Auran.Clinic.Domain.Enums.ClinicalOrderSectionType.File,
            IsEnabled = true, SortOrder = 0
        });
        db.ClinicalOrders.Add(new ClinicalOrder
        {
            Id = orderId, ClinicId = clinicA, VisitId = visitA,
            PatientId = patientA, DoctorId = userId, CreatedByUserId = userId
        });
        db.ClinicalOrderSections.Add(new ClinicalOrderSection
        {
            Id = fileSectionId, ClinicId = clinicA,
            ClinicalOrderId = orderId, SectionDefinitionId = fileDefinitionId
        });
        db.ClinicalOrderAttachments.Add(new ClinicalOrderAttachment
        {
            Id = fileLinkId, ClinicId = clinicA,
            ClinicalOrderId = orderId,
            ClinicalOrderSectionId = fileSectionId, FileId = fileA
        });
        await db.SaveChangesAsync();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Visit_Edit"));
        using (var updateOrder = await client.PutAsJsonAsync(
            "/api/clinical-orders", new { visitId = visitA, sections = Array.Empty<object>() }))
        {
            Assert.Equal(HttpStatusCode.OK, updateOrder.StatusCode);
        }
        Assert.True(await db.ClinicalOrderSections.AsNoTracking()
            .AnyAsync(section => section.Id == fileSectionId &&
                section.SectionDefinitionId == fileDefinitionId));
        Assert.True(await db.ClinicalOrderAttachments.AsNoTracking()
            .AnyAsync(link => link.Id == fileLinkId &&
                link.ClinicalOrderSectionId == fileSectionId && link.FileId == fileA));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Files_Upload"));
        using (var deleteLinkedRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/patient-attachments")
        {
            Content = JsonContent.Create(new { attachmentId = referencedPatientAttachmentId })
        })
        using (var blockedDelete = await client.SendAsync(deleteLinkedRequest))
        {
            Assert.Equal(HttpStatusCode.Conflict, blockedDelete.StatusCode);
            Assert.Contains("attachment_in_use", await blockedDelete.Content.ReadAsStringAsync());
        }
        Assert.True(await db.PatientAttachments.AsNoTracking()
            .AnyAsync(attachment => attachment.Id == referencedPatientAttachmentId));
        Assert.True(await db.ClinicalOrderAttachments.AsNoTracking()
            .AnyAsync(link => link.Id == fileLinkId));
        Assert.Equal(0, await db.AuditLogs.AsNoTracking().CountAsync(
            audit => audit.ClinicId == clinicA &&
                audit.Action == "PatientAttachment.Deleted" &&
                audit.EntityId == referencedPatientAttachmentId.ToString()));

        // Exercise real local storage and database lifecycle for an authorized patient.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Files_Upload", "Files_View"));
        // MIME headers are untrusted. Disguised content must be rejected before
        // saving a FileRecord or a patient attachment.
        using (var disguisedPdf = new MultipartFormDataContent())
        {
            disguisedPdf.Add(new StringContent(patientA.ToString()), "PatientId");
            using var binary = new ByteArrayContent(new byte[] { 0x4D, 0x5A, 0x00, 0x00, 0x90, 0x00 });
            binary.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            disguisedPdf.Add(binary, "File", "report.pdf");
            using var response = await client.PostAsync("/api/patient-attachments", disguisedPdf);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        using (var disguisedImage = new MultipartFormDataContent())
        {
            disguisedImage.Add(new StringContent(patientA.ToString()), "PatientId");
            using var binary = new ByteArrayContent(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31 });
            binary.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            disguisedImage.Add(binary, "File", "scan.png");
            using var response = await client.PostAsync("/api/patient-attachments", disguisedImage);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        using var ownPayload = new MultipartFormDataContent();
        ownPayload.Add(new StringContent(patientA.ToString()), "PatientId");
        using var ownFile = new ByteArrayContent(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2d, 0x31 });
        ownFile.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        ownPayload.Add(ownFile, "File", "lifecycle.pdf");
        using var uploaded = await client.PostAsync("/api/patient-attachments", ownPayload);
        Assert.Equal(HttpStatusCode.Created, uploaded.StatusCode);
        using var result = JsonDocument.Parse(await uploaded.Content.ReadAsStringAsync());
        var fileId = result.RootElement.GetProperty("data").GetProperty("fileId").GetGuid();
        var attachmentId = result.RootElement.GetProperty("data").GetProperty("attachmentId").GetGuid();
        using var downloaded = await client.GetAsync($"/api/patient-attachments/download?fileId={fileId}");
        Assert.Equal(HttpStatusCode.OK, downloaded.StatusCode);
        Assert.True(downloaded.Headers.CacheControl?.NoStore);
        Assert.True(downloaded.Headers.TryGetValues("X-Content-Type-Options", out var sniffOptions));
        Assert.Contains("nosniff", sniffOptions);
        Assert.Equal(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2d, 0x31 }, await downloaded.Content.ReadAsByteArrayAsync());
        var deletionStorageKey = await db.Files.AsNoTracking()
            .Where(file => file.Id == fileId)
            .Select(file => file.StorageKey)
            .SingleAsync();

        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/patient-attachments")
        {
            Content = JsonContent.Create(new { attachmentId })
        };
        using var deleted = await client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        Assert.Equal(1, await db.AuditLogs.AsNoTracking()
            .CountAsync(audit => audit.ClinicId == clinicA &&
                audit.Action == "PatientAttachment.Deleted" &&
                audit.EntityId == attachmentId.ToString()));
        Assert.False(await db.Files.AsNoTracking()
            .AnyAsync(file => file.Id == fileId));
        // The outbox survives the HTTP transaction, even if local cleanup fails.
        Assert.Equal(1, await db.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS Value FROM dbo.FileDeletionOutbox WHERE StorageKey = @key",
            new SqlParameter("@key", deletionStorageKey)).SingleAsync());
        var cleanupWorker = new FileDeletionOutboxWorker(
            _factory.Services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new FileDeletionOutboxOptions()),
            NullLogger<FileDeletionOutboxWorker>.Instance);
        await cleanupWorker.ProcessBatchAsync();
        Assert.Equal(1, await db.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS Value FROM dbo.FileDeletionOutbox WHERE StorageKey = @key AND CompletedAtUtc IS NOT NULL",
            new SqlParameter("@key", deletionStorageKey)).SingleAsync());
        using var gone = await client.GetAsync($"/api/patient-attachments/download?fileId={fileId}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);

        // Specialty requirements are clinic-managed, doctor-specific and checked
        // only on explicit finalization, without silently completing draft notes.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Visit_View"));
        using (var forbiddenPolicy = await client.PutAsJsonAsync(
            "/api/documentation-policies",
            new { specialtyCode = "GENERAL", requiredFields = new[] { "ChiefComplaint", "Diagnosis" } }))
            Assert.Equal(HttpStatusCode.Forbidden, forbiddenPolicy.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Settings_Manage"));
        using (var savedPolicy = await client.PutAsJsonAsync(
            "/api/documentation-policies",
            new { specialtyCode = "GENERAL", requiredFields = new[] { "ChiefComplaint", "Diagnosis" } }))
            Assert.Equal(HttpStatusCode.OK, savedPolicy.StatusCode);

        using (var foreignAssignment = await client.PutAsJsonAsync(
            "/api/documentation-policies/doctors",
            new { doctorId = doctorB, specialtyCode = "GENERAL" }))
            Assert.Equal(HttpStatusCode.NotFound, foreignAssignment.StatusCode);

        using (var assigned = await client.PutAsJsonAsync(
            "/api/documentation-policies/doctors",
            new { doctorId = userId, specialtyCode = "GENERAL" }))
            Assert.Equal(HttpStatusCode.OK, assigned.StatusCode);

        var policyVisit = await db.Visits.SingleAsync(v => v.Id == visitA);
        policyVisit.DocumentationStatus = Auran.Clinic.Domain.Enums.DocumentationStatus.Draft;
        await db.SaveChangesAsync();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Visit_Edit"));
        using (var incomplete = await client.PutAsJsonAsync(
            "/api/pending-documentation/complete",
            new { visitId = visitA, chiefComplaint = "Patient complaint recorded", notes = "Working draft" }))
        {
            Assert.Equal(HttpStatusCode.BadRequest, incomplete.StatusCode);
            Assert.Contains("documentation_required_fields_missing",
                await incomplete.Content.ReadAsStringAsync());
            Assert.Contains("Diagnosis", await incomplete.Content.ReadAsStringAsync());
        }
        Assert.Equal(Auran.Clinic.Domain.Enums.DocumentationStatus.Draft,
            (await db.Visits.AsNoTracking().SingleAsync(v => v.Id == visitA)).DocumentationStatus);

        using (var completedPolicy = await client.PutAsJsonAsync(
            "/api/pending-documentation/complete",
            new { visitId = visitA, chiefComplaint = "Patient complaint recorded",
                  diagnosis = "Clinician-entered assessment" }))
            Assert.Equal(HttpStatusCode.OK, completedPolicy.StatusCode);

        // Explicit negative permission matrix: permission for one clinical
        // capability must not transitively grant unrelated reads or writes.
        var readDenials = new (string Permission, string Path)[]
        {
            ("Patient_View", "/api/pending-documentation?mineOnly=false"),
            ("Patient_View", "/api/queue"),
            ("Patient_View", $"/api/clinical-orders?visitId={visitA}"),
            ("Visit_View", $"/api/patient-attachments?patientId={patientA}"),
            ("Visit_View", "/api/follow-ups?bucket=All"),
            ("Files_View", $"/api/clinical-sessions/active?visitId={visitA}"),
            ("Patient_View", "/api/documentation-policies?specialtyCode=GENERAL")
        };
        foreach (var (permission, path) in readDenials)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer", CreateToken(configuration, userId, clinicA, sessionId, permission));
            using var deniedRead = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.Forbidden, deniedRead.StatusCode);
        }

        var mutationDenials = new (string Permission, HttpMethod Method, string Path)[]
        {
            ("Visit_View", HttpMethod.Put, "/api/clinical-orders"),
            ("Visit_View", HttpMethod.Put, "/api/clinical-sessions/documentation"),
            ("Visit_View", HttpMethod.Put, "/api/pending-documentation/complete"),
            ("Visit_View", HttpMethod.Post, "/api/clinical-sessions/start"),
            ("Settings_View", HttpMethod.Put, "/api/documentation-policies"),
            ("Settings_View", HttpMethod.Put, "/api/documentation-policies/doctors"),
            ("Queue_View", HttpMethod.Put, "/api/queue/move"),
            ("FollowUp_View", HttpMethod.Put, "/api/follow-ups/complete"),
            ("Files_View", HttpMethod.Delete, "/api/patient-attachments")
        };
        foreach (var (permission, method, path) in mutationDenials)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer", CreateToken(configuration, userId, clinicA, sessionId, permission));
            using var deniedMutation = new HttpRequestMessage(method, path)
            {
                Content = JsonContent.Create(new { })
            };
            using var response = await client.SendAsync(deniedMutation);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // Endpoint consumes multipart/form-data; a JSON request is rejected by
        // content negotiation (415) before it can establish an auth contract.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId, "Files_View"));
        using (var multipartWithoutPermission = new MultipartFormDataContent())
        {
            multipartWithoutPermission.Add(new StringContent(patientA.ToString()), "PatientId");
            using var filePart = new ByteArrayContent("%PDF-1"u8.ToArray());
            filePart.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            multipartWithoutPermission.Add(filePart, "File", "restricted.pdf");
            using var uploadDenied = await client.PostAsync(
                "/api/patient-attachments", multipartWithoutPermission);
            Assert.Equal(HttpStatusCode.Forbidden, uploadDenied.StatusCode);
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(configuration, userId, clinicA, sessionId));
        var denied = await client.GetAsync($"/api/visits/history?patientId={patientA}");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    private static string CreateToken(
        IConfiguration config, Guid userId, Guid clinicId, Guid sessionId, params string[] permissions)
    {
        var claims = new List<Claim>
        {
            new("actor_type", "Clinic"),
            new("user_id", userId.ToString()),
            new("clinic_id", clinicId.ToString()),
            new("session_id", sessionId.ToString())
        };
        claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:SigningKey"]!));
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"], audience: config["Jwt:Audience"],
            claims: claims, expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
