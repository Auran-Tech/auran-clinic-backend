using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Attachments;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Files;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.Attachments;

public sealed class PatientAttachmentService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext,
    IFileStorageService fileStorage,
    IAuditService auditService) : IPatientAttachmentService
{
    private const long MaxFileSize = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "application/pdf"
    };

    public async Task<IReadOnlyList<PatientAttachmentResponse>> ListAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        return await (
            from attachment in dbContext.PatientAttachments.AsNoTracking()
            join file in dbContext.Files.AsNoTracking() on attachment.FileId equals file.Id
            where attachment.PatientId == patientId
            orderby file.UploadedAtUtc descending
            select new PatientAttachmentResponse(
                attachment.Id,
                file.Id,
                attachment.PatientId,
                file.OriginalName,
                file.ContentType,
                file.Size,
                attachment.Category,
                attachment.Notes,
                file.UploadedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<PatientAttachmentResult> UploadAsync(
        Guid patientId,
        string originalName,
        string contentType,
        long size,
        Stream content,
        string? category,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentActor(out var userId, out var clinicId))
            return new PatientAttachmentResult(PatientAttachmentOutcome.Unauthenticated);

        if (patientId == Guid.Empty ||
            string.IsNullOrWhiteSpace(originalName) ||
            size <= 0 ||
            size > MaxFileSize ||
            !AllowedContentTypes.Contains(contentType))
        {
            return new PatientAttachmentResult(
                PatientAttachmentOutcome.ValidationError,
                Error: "File must be JPEG, PNG, WebP, or PDF and no larger than 10 MB.");
        }

        var patientExists = await dbContext.Patients.AsNoTracking()
            .AnyAsync(patient => patient.Id == patientId, cancellationToken);
        if (!patientExists)
            return new PatientAttachmentResult(PatientAttachmentOutcome.PatientNotFound);

        var safeOriginalName = Path.GetFileName(originalName.Trim());
        var stored = await fileStorage.SaveAsync(
            content,
            safeOriginalName,
            contentType,
            cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;
            var file = new FileRecord
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicId,
                OriginalName = safeOriginalName,
                StoredName = stored.StoredName,
                ContentType = contentType,
                Size = stored.Size,
                StorageProvider = stored.Provider,
                StorageKey = stored.StorageKey,
                UploadedAtUtc = now,
                UploadedByUserId = userId,
                CreatedDate = now,
                CreateByUserId = userId
            };

            var attachment = new PatientAttachment
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicId,
                PatientId = patientId,
                FileId = file.Id,
                Category = Clean(category),
                Notes = Clean(notes),
                CreatedDate = now,
                CreateByUserId = userId
            };

            dbContext.Files.Add(file);
            dbContext.PatientAttachments.Add(attachment);
            await dbContext.SaveChangesAsync(cancellationToken);

            await auditService.WriteAsync(
                "PatientAttachment.Uploaded",
                nameof(PatientAttachment),
                attachment.Id.ToString(),
                new Dictionary<string, object?>
                {
                    ["patientId"] = patientId,
                    ["fileId"] = file.Id,
                    ["contentType"] = file.ContentType,
                    ["size"] = file.Size
                },
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new PatientAttachmentResult(
                PatientAttachmentOutcome.Success,
                Map(attachment, file));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            await fileStorage.DeleteAsync(stored.StorageKey, cancellationToken);
            throw;
        }
    }

    public async Task<PatientAttachmentDownload?> DownloadAsync(
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        var file = await (
            from attachment in dbContext.PatientAttachments.AsNoTracking()
            join fileRecord in dbContext.Files.AsNoTracking() on attachment.FileId equals fileRecord.Id
            where fileRecord.Id == fileId
            select fileRecord)
            .SingleOrDefaultAsync(cancellationToken);

        if (file is null)
            return null;

        var stream = await fileStorage.OpenReadAsync(file.StorageKey, cancellationToken);
        return new PatientAttachmentDownload(stream, file.ContentType, file.OriginalName);
    }

    public async Task<bool> DeleteAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentActor(out _, out _))
            return false;

        var attachment = await dbContext.PatientAttachments
            .SingleOrDefaultAsync(item => item.Id == attachmentId, cancellationToken);
        if (attachment is null)
            return false;

        var file = await dbContext.Files
            .SingleOrDefaultAsync(item => item.Id == attachment.FileId, cancellationToken);

        dbContext.PatientAttachments.Remove(attachment);

        var isReferencedByClinicalOrder = await dbContext.ClinicalOrderAttachments.AsNoTracking()
            .AnyAsync(item => item.FileId == attachment.FileId, cancellationToken);

        if (file is not null && !isReferencedByClinicalOrder)
            dbContext.Files.Remove(file);

        await dbContext.SaveChangesAsync(cancellationToken);

        if (file is not null && !isReferencedByClinicalOrder)
            await fileStorage.DeleteAsync(file.StorageKey, cancellationToken);

        await auditService.WriteAsync(
            "PatientAttachment.Deleted",
            nameof(PatientAttachment),
            attachment.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["patientId"] = attachment.PatientId,
                ["fileId"] = attachment.FileId
            },
            cancellationToken);

        return true;
    }

    private bool TryGetCurrentActor(out Guid userId, out Guid clinicId)
    {
        if (currentUserContext.IsAuthenticated &&
            currentUserContext.UserId is Guid currentUserId &&
            currentUserContext.ClinicId is Guid currentClinicId)
        {
            userId = currentUserId;
            clinicId = currentClinicId;
            return true;
        }

        userId = Guid.Empty;
        clinicId = Guid.Empty;
        return false;
    }

    private static PatientAttachmentResponse Map(
        PatientAttachment attachment,
        FileRecord file) =>
        new(
            attachment.Id,
            file.Id,
            attachment.PatientId,
            file.OriginalName,
            file.ContentType,
            file.Size,
            attachment.Category,
            attachment.Notes,
            file.UploadedAtUtc);

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
