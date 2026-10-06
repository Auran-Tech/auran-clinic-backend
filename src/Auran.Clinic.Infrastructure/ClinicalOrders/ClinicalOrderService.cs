using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.ClinicalOrders;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.ClinicalOrders;

public sealed class ClinicalOrderService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext,
    IAuditService auditService) : IClinicalOrderService
{
    public async Task<IReadOnlyList<ClinicalOrderSectionDefinitionResponse>> ListDefinitionsAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ClinicalOrderSectionDefinitions.AsNoTracking()
            .Where(definition =>
                definition.IsEnabled &&
                (definition.SectionType == ClinicalOrderSectionType.Text ||
                 definition.SectionType == ClinicalOrderSectionType.Structured))
            .OrderBy(definition => definition.SortOrder)
            .ThenBy(definition => definition.Name)
            .Select(definition => new ClinicalOrderSectionDefinitionResponse(
                definition.Id,
                definition.Name,
                definition.SectionType.ToString(),
                definition.SortOrder))
            .ToListAsync(cancellationToken);
    }

    public async Task<ClinicalOrderResponse?> GetForVisitAsync(
        Guid visitId,
        CancellationToken cancellationToken = default)
    {
        var order = await dbContext.ClinicalOrders.AsNoTracking()
            .Where(item => item.VisitId == visitId)
            .OrderBy(item => item.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (order is null)
            return null;

        return await MapAsync(order, cancellationToken);
    }

    public async Task<ClinicalOrderResult> SaveAsync(
        SaveClinicalOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentActor(out var userId))
            return new ClinicalOrderResult(ClinicalOrderOutcome.Unauthenticated);

        var visit = await dbContext.Visits
            .SingleOrDefaultAsync(item => item.Id == request.VisitId, cancellationToken);
        if (visit is null)
            return new ClinicalOrderResult(ClinicalOrderOutcome.VisitNotFound);

        if (visit.DoctorId != userId && !currentUserContext.IsSuperUser)
            return new ClinicalOrderResult(ClinicalOrderOutcome.Forbidden);

        var requestedDefinitionIds = request.Sections
            .Select(section => section.SectionDefinitionId)
            .Distinct()
            .ToArray();

        var definitions = requestedDefinitionIds.Length == 0
            ? new List<ClinicalOrderSectionDefinition>()
            : await dbContext.ClinicalOrderSectionDefinitions
                .Where(definition =>
                    requestedDefinitionIds.Contains(definition.Id) &&
                    definition.IsEnabled &&
                    (definition.SectionType == ClinicalOrderSectionType.Text ||
                     definition.SectionType == ClinicalOrderSectionType.Structured))
                .ToListAsync(cancellationToken);

        if (definitions.Count != requestedDefinitionIds.Length)
        {
            return new ClinicalOrderResult(
                ClinicalOrderOutcome.InvalidSectionDefinition,
                Error: "One or more clinical order sections are invalid or disabled.");
        }

        var definitionMap = definitions.ToDictionary(item => item.Id);
        var now = DateTime.UtcNow;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var order = await dbContext.ClinicalOrders
                .Where(item => item.VisitId == visit.Id)
                .OrderBy(item => item.CreatedDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (order is null)
            {
                order = new ClinicalOrder
                {
                    Id = Guid.NewGuid(),
                    ClinicId = visit.ClinicId,
                    VisitId = visit.Id,
                    PatientId = visit.PatientId,
                    DoctorId = visit.DoctorId,
                    CreatedByUserId = userId,
                    CreatedDate = now,
                    CreateByUserId = userId
                };
                dbContext.ClinicalOrders.Add(order);
            }
            else
            {
                order.DoctorId = visit.DoctorId;
                order.UpdatedDate = now;
                order.UpdatedByUserId = userId;

                var oldSections = await dbContext.ClinicalOrderSections
                    .Where(section => section.ClinicalOrderId == order.Id)
                    .ToListAsync(cancellationToken);

                if (oldSections.Count > 0)
                {
                    var oldSectionIds = oldSections.Select(section => section.Id).ToArray();
                    var oldItems = await dbContext.ClinicalOrderItems
                        .Where(item => oldSectionIds.Contains(item.ClinicalOrderSectionId))
                        .ToListAsync(cancellationToken);

                    dbContext.ClinicalOrderItems.RemoveRange(oldItems);
                    dbContext.ClinicalOrderSections.RemoveRange(oldSections);
                }
            }

            foreach (var requestSection in request.Sections)
            {
                var definition = definitionMap[requestSection.SectionDefinitionId];
                var section = new ClinicalOrderSection
                {
                    Id = Guid.NewGuid(),
                    ClinicId = visit.ClinicId,
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
                        ClinicId = visit.ClinicId,
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
                    ["visitId"] = visit.Id,
                    ["sectionCount"] = request.Sections.Count
                },
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return new ClinicalOrderResult(
                ClinicalOrderOutcome.Success,
                await MapAsync(order, cancellationToken));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
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
            : await dbContext.ClinicalOrderItems.AsNoTracking()
                .Where(item => sectionIds.Contains(item.ClinicalOrderSectionId))
                .OrderBy(item => item.CreatedDate)
                .ToListAsync(cancellationToken);

        var responseSections = sections.Select(item =>
            new ClinicalOrderSectionResponse(
                item.Section.Id,
                item.Definition.Id,
                item.Definition.Name,
                item.Definition.SectionType.ToString(),
                item.Section.SortOrder,
                item.Section.TextValue,
                items
                    .Where(orderItem => orderItem.ClinicalOrderSectionId == item.Section.Id)
                    .Select(orderItem => new ClinicalOrderItemResponse(
                        orderItem.Id,
                        orderItem.Name,
                        orderItem.DetailsJson))
                    .ToList()))
            .ToList();

        return new ClinicalOrderResponse(
            order.Id,
            order.VisitId,
            order.PatientId,
            order.DoctorId,
            responseSections);
    }

    private bool TryGetCurrentActor(out Guid userId)
    {
        if (currentUserContext.IsAuthenticated && currentUserContext.UserId is Guid currentUserId)
        {
            userId = currentUserId;
            return true;
        }

        userId = Guid.Empty;
        return false;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
