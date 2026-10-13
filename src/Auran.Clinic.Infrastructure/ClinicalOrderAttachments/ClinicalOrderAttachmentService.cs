using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.ClinicalOrderAttachments;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.ClinicalOrderAttachments;

public sealed class ClinicalOrderAttachmentService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext,
    IAuditService auditService) : IClinicalOrderAttachmentService
{
    public async Task<ClinicalOrderAttachmentWorkspaceResponse?> GetWorkspaceAsync(
        Guid visitId,
        CancellationToken cancellationToken = default)
    {
        var visit = await dbContext.Visits.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == visitId, cancellationToken);

        if (visit is null)
            return null;

        var definitions = await dbContext.ClinicalOrderSectionDefinitions.AsNoTracking()
            .Where(definition =>
                definition.IsEnabled &&
                (definition.SectionType == ClinicalOrderSectionType.Image ||
                 definition.SectionType == ClinicalOrderSectionType.File))
            .OrderBy(definition => definition.SortOrder)
            .ThenBy(definition => definition.Name)
            .ToListAsync(cancellationToken);

        var files = await (
            from attachment in dbContext.PatientAttachments.AsNoTracking()
            join file in dbContext.Files.AsNoTracking() on attachment.FileId equals file.Id
            where attachment.PatientId == visit.PatientId
            orderby file.UploadedAtUtc descending
            select new ClinicalOrderAttachmentFileResponse(
                attachment.Id,
                file.Id,
                file.OriginalName,
                file.ContentType,
                file.Size,
                attachment.Category,
                attachment.Notes,
                file.UploadedAtUtc))
            .ToListAsync(cancellationToken);

        var order = await dbContext.ClinicalOrders.AsNoTracking()
            .Where(item => item.VisitId == visit.Id)
            .OrderBy(item => item.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);

        var links = new List<ClinicalOrderAttachmentLinkResponse>();

        if (order is not null)
        {
            links = await (
                from link in dbContext.ClinicalOrderAttachments.AsNoTracking()
                join section in dbContext.ClinicalOrderSections.AsNoTracking()
                    on link.ClinicalOrderSectionId equals (Guid?)section.Id
                join definition in dbContext.ClinicalOrderSectionDefinitions.AsNoTracking()
                    on section.SectionDefinitionId equals definition.Id
                join file in dbContext.Files.AsNoTracking() on link.FileId equals file.Id
                where
                    link.ClinicalOrderId == order.Id &&
                    link.ClinicalOrderSectionId != null
                orderby definition.SortOrder, file.OriginalName
                select new ClinicalOrderAttachmentLinkResponse(
                    link.Id,
                    file.Id,
                    definition.Id,
                    definition.Name,
                    definition.SectionType.ToString(),
                    file.OriginalName,
                    file.ContentType,
                    file.Size))
                .ToListAsync(cancellationToken);
        }

        return new ClinicalOrderAttachmentWorkspaceResponse(
            visit.Id,
            visit.PatientId,
            order?.Id,
            definitions
                .Select(definition => new ClinicalOrderAttachmentSectionResponse(
                    definition.Id,
                    definition.Name,
                    definition.SectionType.ToString(),
                    definition.SortOrder))
                .ToList(),
            files,
            links);
    }

    public async Task<ClinicalOrderAttachmentResult> LinkAsync(
        LinkClinicalOrderAttachmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId, out var clinicId))
            return new ClinicalOrderAttachmentResult(ClinicalOrderAttachmentOutcome.Unauthenticated);

        var visit = await dbContext.Visits
            .SingleOrDefaultAsync(item => item.Id == request.VisitId, cancellationToken);

        if (visit is null)
            return new ClinicalOrderAttachmentResult(ClinicalOrderAttachmentOutcome.VisitNotFound);

        if (visit.Status != VisitStatus.Open)
            return new ClinicalOrderAttachmentResult(ClinicalOrderAttachmentOutcome.VisitClosed);

        if (visit.DoctorId != userId && !currentUserContext.IsSuperUser)
            return new ClinicalOrderAttachmentResult(ClinicalOrderAttachmentOutcome.Forbidden);

        var definition = await dbContext.ClinicalOrderSectionDefinitions
            .SingleOrDefaultAsync(
                item =>
                    item.Id == request.SectionDefinitionId &&
                    item.IsEnabled &&
                    (item.SectionType == ClinicalOrderSectionType.Image ||
                     item.SectionType == ClinicalOrderSectionType.File),
                cancellationToken);

        if (definition is null)
        {
            return new ClinicalOrderAttachmentResult(
                ClinicalOrderAttachmentOutcome.InvalidSection,
                Error: "Clinical order section is invalid, disabled, or not an attachment section.");
        }

        var file = await (
            from attachment in dbContext.PatientAttachments.AsNoTracking()
            join fileRecord in dbContext.Files.AsNoTracking() on attachment.FileId equals fileRecord.Id
            where
                attachment.PatientId == visit.PatientId &&
                fileRecord.Id == request.FileId
            select fileRecord)
            .SingleOrDefaultAsync(cancellationToken);

        if (file is null)
            return new ClinicalOrderAttachmentResult(ClinicalOrderAttachmentOutcome.FileNotFound);

        if (definition.SectionType == ClinicalOrderSectionType.Image &&
            !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return new ClinicalOrderAttachmentResult(
                ClinicalOrderAttachmentOutcome.InvalidSection,
                Error: "Image clinical order sections only accept image attachments.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var order = await dbContext.ClinicalOrders
                .Where(item => item.VisitId == visit.Id)
                .OrderBy(item => item.CreatedDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (order is null)
            {
                var now = DateTime.UtcNow;
                order = new ClinicalOrder
                {
                    Id = Guid.NewGuid(),
                    ClinicId = clinicId,
                    VisitId = visit.Id,
                    PatientId = visit.PatientId,
                    DoctorId = visit.DoctorId,
                    CreatedByUserId = userId,
                    CreatedDate = now,
                    CreateByUserId = userId
                };
                dbContext.ClinicalOrders.Add(order);
            }

            var section = await dbContext.ClinicalOrderSections
                .SingleOrDefaultAsync(
                    item =>
                        item.ClinicalOrderId == order.Id &&
                        item.SectionDefinitionId == definition.Id,
                    cancellationToken);

            if (section is null)
            {
                section = new ClinicalOrderSection
                {
                    Id = Guid.NewGuid(),
                    ClinicId = clinicId,
                    ClinicalOrderId = order.Id,
                    SectionDefinitionId = definition.Id,
                    SortOrder = definition.SortOrder,
                    CreatedDate = DateTime.UtcNow,
                    CreateByUserId = userId
                };
                dbContext.ClinicalOrderSections.Add(section);
            }
            else
            {
                var duplicate = await dbContext.ClinicalOrderAttachments.AsNoTracking()
                    .AnyAsync(
                        item =>
                            item.ClinicalOrderId == order.Id &&
                            item.ClinicalOrderSectionId == section.Id &&
                            item.FileId == file.Id,
                        cancellationToken);

                if (duplicate)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new ClinicalOrderAttachmentResult(
                        ClinicalOrderAttachmentOutcome.Conflict,
                        Error: "This file is already linked to the selected clinical order section.");
                }
            }

            var link = new ClinicalOrderAttachment
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicId,
                ClinicalOrderId = order.Id,
                ClinicalOrderSectionId = section.Id,
                FileId = file.Id,
                CreatedDate = DateTime.UtcNow,
                CreateByUserId = userId
            };

            dbContext.ClinicalOrderAttachments.Add(link);
            await dbContext.SaveChangesAsync(cancellationToken);

            await auditService.WriteAsync(
                "ClinicalOrderAttachment.Linked",
                nameof(ClinicalOrderAttachment),
                link.Id.ToString(),
                new Dictionary<string, object?>
                {
                    ["visitId"] = visit.Id,
                    ["fileId"] = file.Id,
                    ["sectionDefinitionId"] = definition.Id
                },
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new ClinicalOrderAttachmentResult(
                ClinicalOrderAttachmentOutcome.Success,
                await GetWorkspaceAsync(visit.Id, cancellationToken));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ClinicalOrderAttachmentResult> DeleteAsync(
        DeleteClinicalOrderAttachmentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId, out _))
            return new ClinicalOrderAttachmentResult(ClinicalOrderAttachmentOutcome.Unauthenticated);

        var link = await dbContext.ClinicalOrderAttachments
            .SingleOrDefaultAsync(item => item.Id == request.LinkId, cancellationToken);

        if (link is null)
            return new ClinicalOrderAttachmentResult(ClinicalOrderAttachmentOutcome.LinkNotFound);

        var order = await dbContext.ClinicalOrders
            .SingleOrDefaultAsync(item => item.Id == link.ClinicalOrderId, cancellationToken);

        if (order is null)
            return new ClinicalOrderAttachmentResult(ClinicalOrderAttachmentOutcome.LinkNotFound);

        var visit = await dbContext.Visits
            .SingleOrDefaultAsync(item => item.Id == order.VisitId, cancellationToken);

        if (visit is null)
            return new ClinicalOrderAttachmentResult(ClinicalOrderAttachmentOutcome.VisitNotFound);

        if (visit.Status != VisitStatus.Open)
            return new ClinicalOrderAttachmentResult(ClinicalOrderAttachmentOutcome.VisitClosed);

        if (visit.DoctorId != userId && !currentUserContext.IsSuperUser)
            return new ClinicalOrderAttachmentResult(ClinicalOrderAttachmentOutcome.Forbidden);

        var sectionId = link.ClinicalOrderSectionId;
        var fileId = link.FileId;

        dbContext.ClinicalOrderAttachments.Remove(link);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (sectionId.HasValue)
        {
            var hasOtherLinks = await dbContext.ClinicalOrderAttachments.AsNoTracking()
                .AnyAsync(
                    item => item.ClinicalOrderSectionId == sectionId.Value,
                    cancellationToken);

            if (!hasOtherLinks)
            {
                var section = await dbContext.ClinicalOrderSections
                    .SingleOrDefaultAsync(item => item.Id == sectionId.Value, cancellationToken);

                if (section is not null)
                {
                    var definition = await dbContext.ClinicalOrderSectionDefinitions.AsNoTracking()
                        .SingleOrDefaultAsync(
                            item => item.Id == section.SectionDefinitionId,
                            cancellationToken);

                    if (definition?.SectionType is ClinicalOrderSectionType.Image or ClinicalOrderSectionType.File)
                    {
                        dbContext.ClinicalOrderSections.Remove(section);
                        await dbContext.SaveChangesAsync(cancellationToken);
                    }
                }
            }
        }

        await auditService.WriteAsync(
            "ClinicalOrderAttachment.Unlinked",
            nameof(ClinicalOrderAttachment),
            request.LinkId.ToString(),
            new Dictionary<string, object?>
            {
                ["visitId"] = visit.Id,
                ["fileId"] = fileId
            },
            cancellationToken);

        return new ClinicalOrderAttachmentResult(
            ClinicalOrderAttachmentOutcome.Success,
            await GetWorkspaceAsync(visit.Id, cancellationToken));
    }

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
}
