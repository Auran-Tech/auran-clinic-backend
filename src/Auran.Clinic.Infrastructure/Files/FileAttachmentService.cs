using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Files;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Auran.Clinic.Infrastructure.Files;

public sealed class FileAttachmentService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext,
    IFileStorage fileStorage,
    IAuditService auditService,
    IOptions<FileStorageOptions> options) : IFileAttachmentService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/webp",
        "text/plain",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    private readonly FileStorageOptions _options = options.Value;

    public async Task<IReadOnlyCollection<FileAttachmentResponse>?> ListPatientFilesAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var patientExists = await dbContext.Patients
            .AsNoTracking()
            .AnyAsync(item => item.Id == patientId, cancellationToken);
        if (!patientExists)
            return null;

        return await (
                from attachment in dbContext.PatientAttachments.AsNoTracking()
                join file in dbContext.Files.AsNoTracking()
                    on attachment.FileId equals file.Id
                where attachment.PatientId == patientId
                orderby file.UploadedAtUtc descending
                select new FileAttachmentResponse(
                    file.Id,
                    file.OriginalName,
                    file.ContentType,
                    file.Size,
                    file.UploadedAtUtc,
                    attachment.Category,
                    attachment.Notes))
            .ToListAsync(cancellationToken);
    }

    public async Task<FileAttachmentResponse?> UploadPatientFileAsync(
        PatientAttachmentUploadMetadata metadata,
        Stream content,
        string originalName,
        string contentType,
        long size,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId, out var clinicId))
            return null;

        if (!IsAccepted(contentType, size))
            return null;

        var patientExists = await dbContext.Patients
            .AsNoTracking()
            .AnyAsync(item => item.Id == metadata.PatientId, cancellationToken);
        if (!patientExists)
            return null;

        var stored = await fileStorage.SaveAsync(
            content,
            originalName,
            contentType,
            cancellationToken);

        var now = DateTime.UtcNow;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var file = CreateFileRecord(
                clinicId,
                userId,
                stored,
                originalName,
                contentType,
                size,
                now);

            dbContext.Files.Add(file);
            dbContext.PatientAttachments.Add(new PatientAttachment
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicId,
                PatientId = metadata.PatientId,
                FileId = file.Id,
                Category = Clean(metadata.Category),
                Notes = Clean(metadata.Notes),
                CreatedDate = now,
                CreateByUserId = userId
            });

            await dbContext.SaveChangesAsync(cancellationToken);

            await auditService.WriteAsync(
                "Patient.FileUploaded",
                nameof(PatientAttachment),
                file.Id.ToString(),
                new Dictionary<string, object?>
                {
                    ["patientId"] = metadata.PatientId,
                    ["originalName"] = file.OriginalName,
                    ["size"] = file.Size
                },
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new FileAttachmentResponse(
                file.Id,
                file.OriginalName,
                file.ContentType,
                file.Size,
                file.UploadedAtUtc,
                Clean(metadata.Category),
                Clean(metadata.Notes));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            await fileStorage.DeleteAsync(stored.StorageKey, cancellationToken);
            throw;
        }
    }

    public async Task<FileAttachmentResponse?> UploadClinicalOrderFileAsync(
        ClinicalOrderAttachmentUploadMetadata metadata,
        Stream content,
        string originalName,
        string contentType,
        long size,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId, out var clinicId))
            return null;

        if (!IsAccepted(contentType, size))
            return null;

        var order = await dbContext.ClinicalOrders
            .SingleOrDefaultAsync(item => item.VisitId == metadata.VisitId, cancellationToken);
        if (order is null)
            return null;

        Guid? sectionId = null;
        if (!string.IsNullOrWhiteSpace(metadata.DefinitionCode))
        {
            var code = metadata.DefinitionCode.Trim().ToUpperInvariant();
            sectionId = await (
                    from section in dbContext.ClinicalOrderSections.AsNoTracking()
                    join definition in dbContext.ClinicalOrderSectionDefinitions.AsNoTracking()
                        on section.SectionDefinitionId equals definition.Id
                    where section.ClinicalOrderId == order.Id && definition.Code == code
                    select (Guid?)section.Id)
                .SingleOrDefaultAsync(cancellationToken);

            if (!sectionId.HasValue)
                return null;
        }

        var stored = await fileStorage.SaveAsync(
            content,
            originalName,
            contentType,
            cancellationToken);

        var now = DateTime.UtcNow;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var file = CreateFileRecord(
                clinicId,
                userId,
                stored,
                originalName,
                contentType,
                size,
                now);

            dbContext.Files.Add(file);
            dbContext.ClinicalOrderAttachments.Add(new ClinicalOrderAttachment
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicId,
                ClinicalOrderId = order.Id,
                ClinicalOrderSectionId = sectionId,
                FileId = file.Id,
                CreatedDate = now,
                CreateByUserId = userId
            });

            await dbContext.SaveChangesAsync(cancellationToken);

            await auditService.WriteAsync(
                "ClinicalOrder.FileUploaded",
                nameof(ClinicalOrderAttachment),
                file.Id.ToString(),
                new Dictionary<string, object?>
                {
                    ["visitId"] = metadata.VisitId,
                    ["definitionCode"] = metadata.DefinitionCode,
                    ["originalName"] = file.OriginalName
                },
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new FileAttachmentResponse(
                file.Id,
                file.OriginalName,
                file.ContentType,
                file.Size,
                file.UploadedAtUtc,
                metadata.DefinitionCode,
                null);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            await fileStorage.DeleteAsync(stored.StorageKey, cancellationToken);
            throw;
        }
    }

    public async Task<StoredFileContent?> OpenFileAsync(
        Guid fileId,
        CancellationToken cancellationToken = default)
    {
        var file = await dbContext.Files
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == fileId, cancellationToken);
        if (file is null)
            return null;

        var content = await fileStorage.OpenReadAsync(file.StorageKey, cancellationToken);
        if (content is null)
            return null;

        return new StoredFileContent(content, file.ContentType, file.OriginalName);
    }

    private bool IsAccepted(string contentType, long size) =>
        size > 0 &&
        size <= _options.MaxFileSizeBytes &&
        AllowedContentTypes.Contains(contentType);

    private static FileRecord CreateFileRecord(
        Guid clinicId,
        Guid userId,
        StoredFileResult stored,
        string originalName,
        string contentType,
        long size,
        DateTime now) =>
        new()
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            OriginalName = Path.GetFileName(originalName),
            StoredName = stored.StoredName,
            ContentType = contentType,
            Size = size,
            StorageProvider = stored.Provider,
            StorageKey = stored.StorageKey,
            UploadedAtUtc = now,
            UploadedByUserId = userId,
            CreatedDate = now,
            CreateByUserId = userId
        };

    private bool TryGetActor(out Guid userId, out Guid clinicId)
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

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
