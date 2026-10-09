using Auran.Clinic.Application.Abstractions;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.ClinicalOrders;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auran.Clinic.Infrastructure.ClinicalOrders;

public sealed class ClinicalOrderSectionConfigurationService(
    AuranClinicDbContext dbContext,
    ICurrentUserContext currentUserContext,
    IAuditService auditService) : IClinicalOrderSectionConfigurationService
{
    public async Task<ClinicalOrderSectionAdminConfigurationResponse> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var definitions = await dbContext.ClinicalOrderSectionDefinitions.AsNoTracking()
            .OrderBy(definition => definition.SortOrder)
            .ThenBy(definition => definition.Name)
            .ToListAsync(cancellationToken);

        var definitionsWithData = (await dbContext.ClinicalOrderSections.AsNoTracking()
                .Select(section => section.SectionDefinitionId)
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToHashSet();

        return new ClinicalOrderSectionAdminConfigurationResponse(
            definitions.Select(definition => new ClinicalOrderSectionAdminResponse(
                definition.Id,
                definition.Name,
                definition.SectionType.ToString(),
                definition.SortOrder,
                definition.IsEnabled,
                definitionsWithData.Contains(definition.Id)))
            .ToList());
    }

    public async Task<ClinicalOrderSectionConfigurationResult> CreateAsync(
        CreateClinicalOrderSectionDefinitionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId, out var clinicId))
            return new ClinicalOrderSectionConfigurationResult(
                ClinicalOrderSectionConfigurationOutcome.Unauthenticated);

        if (!Enum.TryParse<ClinicalOrderSectionType>(request.SectionType, true, out var sectionType))
        {
            return new ClinicalOrderSectionConfigurationResult(
                ClinicalOrderSectionConfigurationOutcome.ValidationError,
                Error: "Unknown clinical order section type.");
        }

        var now = DateTime.UtcNow;
        var definition = new ClinicalOrderSectionDefinition
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            Name = request.Name.Trim(),
            SectionType = sectionType,
            SortOrder = request.SortOrder,
            IsEnabled = true,
            CreatedDate = now,
            CreateByUserId = userId
        };

        dbContext.ClinicalOrderSectionDefinitions.Add(definition);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "ClinicalOrderSectionDefinition.Created",
            nameof(ClinicalOrderSectionDefinition),
            definition.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["sectionType"] = definition.SectionType.ToString(),
                ["name"] = definition.Name
            },
            cancellationToken);

        return new ClinicalOrderSectionConfigurationResult(
            ClinicalOrderSectionConfigurationOutcome.Success,
            await GetAsync(cancellationToken));
    }

    public async Task<ClinicalOrderSectionConfigurationResult> UpdateAsync(
        UpdateClinicalOrderSectionDefinitionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActor(out var userId, out _))
            return new ClinicalOrderSectionConfigurationResult(
                ClinicalOrderSectionConfigurationOutcome.Unauthenticated);

        var definition = await dbContext.ClinicalOrderSectionDefinitions
            .SingleOrDefaultAsync(
                item => item.Id == request.SectionDefinitionId,
                cancellationToken);

        if (definition is null)
        {
            return new ClinicalOrderSectionConfigurationResult(
                ClinicalOrderSectionConfigurationOutcome.NotFound);
        }

        if (!Enum.TryParse<ClinicalOrderSectionType>(request.SectionType, true, out var requestedType))
        {
            return new ClinicalOrderSectionConfigurationResult(
                ClinicalOrderSectionConfigurationOutcome.ValidationError,
                Error: "Unknown clinical order section type.");
        }

        var hasData = await dbContext.ClinicalOrderSections.AsNoTracking()
            .AnyAsync(
                section => section.SectionDefinitionId == definition.Id,
                cancellationToken);

        if (hasData && definition.SectionType != requestedType)
        {
            return new ClinicalOrderSectionConfigurationResult(
                ClinicalOrderSectionConfigurationOutcome.Conflict,
                Error: "Section type cannot be changed after clinical order data has been recorded.");
        }

        definition.Name = request.Name.Trim();
        definition.SectionType = requestedType;
        definition.SortOrder = request.SortOrder;
        definition.IsEnabled = request.IsEnabled;
        definition.UpdatedDate = DateTime.UtcNow;
        definition.UpdatedByUserId = userId;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            "ClinicalOrderSectionDefinition.Updated",
            nameof(ClinicalOrderSectionDefinition),
            definition.Id.ToString(),
            new Dictionary<string, object?>
            {
                ["sectionType"] = definition.SectionType.ToString(),
                ["name"] = definition.Name,
                ["isEnabled"] = definition.IsEnabled,
                ["hasData"] = hasData
            },
            cancellationToken);

        return new ClinicalOrderSectionConfigurationResult(
            ClinicalOrderSectionConfigurationOutcome.Success,
            await GetAsync(cancellationToken));
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
