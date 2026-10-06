using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.ClinicalOrders;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.ClinicalOrders;

public sealed class ClinicalOrderService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext,
    IAuditService auditService) : IClinicalOrderService
{
    public async Task<IReadOnlyCollection<ClinicalOrderSectionDefinitionResponse>> GetDefinitionsAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ClinicalOrderSectionDefinitions
            .AsNoTracking()
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .Select(item => new ClinicalOrderSectionDefinitionResponse(
                item.Code,
                item.Name,
                item.SectionType,
                item.SortOrder,
                item.IsEnabled))
            .ToListAsync(cancellationToken);
    }

    public async Task<ClinicalOrderResponse?> GetByVisitAsync(
        Guid visitId,
        CancellationToken cancellationToken = default)
    {
        var order = await dbContext.ClinicalOrders
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.VisitId == visitId, cancellationToken);

        if (order is null)
            return null;

        return await MapAsync(order, cancellationToken);
    }

    public async Task<ClinicalOrderResponse?> SaveAsync(
        SaveClinicalOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!currentUserContext.UserId.HasValue || !currentUserContext.ClinicId.HasValue)
            return null;

        var userId = currentUserContext.UserId.Value;
        var clinicId = currentUserContext.ClinicId.Value;

        var visit = await dbContext.Visits
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.VisitId, cancellationToken);
        if (visit is null)
            return null;

        var requestedCodes = request.Sections
            .Select(item => item.DefinitionCode.Trim().ToUpperInvariant())
            .ToArray();

        var definitions = await dbContext.ClinicalOrderSectionDefinitions
            .AsNoTracking()
            .Where(item => requestedCodes.Contains(item.Code) && item.IsEnabled)
            .ToListAsync(cancellationToken);

        if (definitions.Count != requestedCodes.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            return null;

        var order = await dbContext.ClinicalOrders
            .SingleOrDefaultAsync(item => item.VisitId == request.VisitId, cancellationToken);

        var now = DateTime.UtcNow;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            if (order is null)
            {
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
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            var existingSections = await dbContext.ClinicalOrderSections
                .Where(item => item.ClinicalOrderId == order.Id)
                .ToListAsync(cancellationToken);

            if (existingSections.Count > 0)
            {
                var sectionIds = existingSections.Select(item => item.Id).ToArray();
                var existingItems = await dbContext.ClinicalOrderItems
                    .Where(item => sectionIds.Contains(item.ClinicalOrderSectionId))
                    .ToListAsync(cancellationToken);
                dbContext.ClinicalOrderItems.RemoveRange(existingItems);
                dbContext.ClinicalOrderSections.RemoveRange(existingSections);
            }

            foreach (var requestSection in request.Sections)
            {
                var code = requestSection.DefinitionCode.Trim().ToUpperInvariant();
                var definition = definitions.Single(item =>
                    item.Code.Equals(code, StringComparison.OrdinalIgnoreCase));

                var section = new ClinicalOrderSection
                {
                    Id = Guid.NewGuid(),
                    ClinicId = clinicId,
                    ClinicalOrderId = order.Id,
                    SectionDefinitionId = definition.Id,
                    SortOrder = definition.SortOrder,
                    TextValue = Clean(requestSection.TextValue),
                    CreatedDate = now,
                    CreateByUserId = userId
                };
                dbContext.ClinicalOrderSections.Add(section);

                foreach (var requestItem in requestSection.Items)
                {
                    dbContext.ClinicalOrderItems.Add(new ClinicalOrderItem
                    {
                        Id = Guid.NewGuid(),
                        ClinicId = clinicId,
                        ClinicalOrderSectionId = section.Id,
                        Name = requestItem.Name.Trim(),
                        DetailsJson = Clean(requestItem.DetailsJson),
                        CreatedDate = now,
                        CreateByUserId = userId
                    });
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            await auditService.WriteAsync(
                "ClinicalOrder.Saved",
                nameof(ClinicalOrder),
                order.Id.ToString(),
                new Dictionary<string, object?>
                {
                    ["visitId"] = order.VisitId,
                    ["sectionCount"] = request.Sections.Count
                },
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return await MapAsync(order, cancellationToken);
    }

    public async Task<(IReadOnlyCollection<ClinicalOrderSectionDefinitionResponse>? Definitions, string? Error)> SaveDefinitionsAsync(
        SaveClinicalOrderSectionDefinitionsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!currentUserContext.UserId.HasValue || !currentUserContext.ClinicId.HasValue)
            return (null, "Authentication with clinic scope is required.");

        var userId = currentUserContext.UserId.Value;
        var clinicId = currentUserContext.ClinicId.Value;
        var now = DateTime.UtcNow;

        var requested = request.Sections
            .Select(item => new SaveClinicalOrderSectionDefinitionRequest
            {
                Code = item.Code.Trim().ToUpperInvariant(),
                Name = item.Name.Trim(),
                SectionType = item.SectionType,
                SortOrder = item.SortOrder,
                IsEnabled = item.IsEnabled
            })
            .ToArray();

        var existing = await dbContext.ClinicalOrderSectionDefinitions
            .ToListAsync(cancellationToken);

        var requestedCodes = requested
            .Select(item => item.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in existing.Where(item => !requestedCodes.Contains(item.Code)))
        {
            var isReferenced = await dbContext.ClinicalOrderSections.AsNoTracking()
                .AnyAsync(item => item.SectionDefinitionId == definition.Id, cancellationToken);

            if (isReferenced)
            {
                definition.IsEnabled = false;
                definition.UpdatedDate = now;
                definition.UpdatedByUserId = userId;
            }
            else
            {
                dbContext.ClinicalOrderSectionDefinitions.Remove(definition);
            }
        }

        foreach (var item in requested)
        {
            var definition = existing.SingleOrDefault(existingItem =>
                existingItem.Code.Equals(item.Code, StringComparison.OrdinalIgnoreCase));

            if (definition is null)
            {
                definition = new ClinicalOrderSectionDefinition
                {
                    Id = Guid.NewGuid(),
                    ClinicId = clinicId,
                    Code = item.Code,
                    Name = item.Name,
                    SectionType = item.SectionType,
                    SortOrder = item.SortOrder,
                    IsEnabled = item.IsEnabled,
                    CreatedDate = now,
                    CreateByUserId = userId
                };
                dbContext.ClinicalOrderSectionDefinitions.Add(definition);
            }
            else
            {
                definition.Name = item.Name;
                definition.SectionType = item.SectionType;
                definition.SortOrder = item.SortOrder;
                definition.IsEnabled = item.IsEnabled;
                definition.UpdatedDate = now;
                definition.UpdatedByUserId = userId;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "Settings.ClinicalOrderSectionsUpdated",
            nameof(ClinicalOrderSectionDefinition),
            null,
            new Dictionary<string, object?> { ["sectionCount"] = requested.Length },
            cancellationToken);

        return (await GetDefinitionsAsync(cancellationToken), null);
    }

    private async Task<ClinicalOrderResponse> MapAsync(
        ClinicalOrder order,
        CancellationToken cancellationToken)
    {
        var sections = await (
                from section in dbContext.ClinicalOrderSections.AsNoTracking()
                join definition in dbContext.ClinicalOrderSectionDefinitions.AsNoTracking()
                    on section.SectionDefinitionId equals definition.Id
                where section.ClinicalOrderId == order.Id
                orderby section.SortOrder, definition.Name
                select new
                {
                    Section = section,
                    Definition = definition
                })
            .ToListAsync(cancellationToken);

        var sectionIds = sections.Select(item => item.Section.Id).ToArray();
        var items = sectionIds.Length == 0
            ? new List<ClinicalOrderItem>()
            : await dbContext.ClinicalOrderItems
                .AsNoTracking()
                .Where(item => sectionIds.Contains(item.ClinicalOrderSectionId))
                .OrderBy(item => item.CreatedDate)
                .ToListAsync(cancellationToken);

        return new ClinicalOrderResponse(
            order.Id,
            order.VisitId,
            order.PatientId,
            order.DoctorId,
            order.CreatedDate,
            sections.Select(item => new ClinicalOrderSectionResponse(
                item.Section.Id,
                item.Definition.Code,
                item.Definition.Name,
                item.Definition.SectionType,
                item.Section.SortOrder,
                item.Section.TextValue,
                items.Where(orderItem => orderItem.ClinicalOrderSectionId == item.Section.Id)
                    .Select(orderItem => new ClinicalOrderItemResponse(
                        orderItem.Id,
                        orderItem.Name,
                        orderItem.DetailsJson))
                    .ToArray()))
                .ToArray());
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
