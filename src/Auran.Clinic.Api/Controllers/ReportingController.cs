using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Application.Reporting;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Route("api/reporting")]
[Produces("application/json")]
public sealed class ReportingController(
    IReportingService reportingService,
    IValidator<VisitReportQuery> visitReportValidator) : ControllerBase
{
    [HttpGet("dashboard")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Reports.View)]
    [SwaggerOperation(
        Summary = "Get clinic dashboard summary",
        Description = "Returns tenant-scoped operational KPIs for the clinic's configured local date.",
        OperationId = "Reporting_Dashboard",
        Tags = new[] { "Reporting" })]
    public async Task<ActionResult<BaseResponse<DashboardSummaryResponse>>> Dashboard(
        CancellationToken cancellationToken)
    {
        var data = await reportingService.GetDashboardAsync(cancellationToken);
        return Ok(new BaseResponse<DashboardSummaryResponse>
        {
            Status = true,
            Data = data
        });
    }

    [HttpGet("visits")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Reports.View)]
    [SwaggerOperation(
        Summary = "Get visit report",
        Description = "Returns visit rows filtered by clinic-local date range, doctor, visit status, and documentation status.",
        OperationId = "Reporting_Visits",
        Tags = new[] { "Reporting" })]
    public async Task<ActionResult<BaseResponse<VisitReportResponse>>> Visits(
        [FromQuery] VisitReportQuery query,
        CancellationToken cancellationToken)
    {
        var validation = await visitReportValidator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new BaseResponse<VisitReportResponse>
            {
                Status = false,
                Error = "validation_error"
            });
        }

        var data = await reportingService.GetVisitReportAsync(query, cancellationToken);
        return Ok(new BaseResponse<VisitReportResponse>
        {
            Status = true,
            Data = data
        });
    }

    [HttpGet("visits/export")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Reports.Export)]
    [SwaggerOperation(
        Summary = "Export visit report as CSV",
        Description = "Exports the same filtered visit report as UTF-8 CSV for spreadsheet use.",
        OperationId = "Reporting_ExportVisitsCsv",
        Tags = new[] { "Reporting" })]
    [Produces("text/csv")]
    public async Task<IActionResult> ExportVisits(
        [FromQuery] VisitReportQuery query,
        CancellationToken cancellationToken)
    {
        var validation = await visitReportValidator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            return BadRequest();

        var export = await reportingService.ExportVisitReportCsvAsync(query, cancellationToken);
        return File(export.Content, export.ContentType, export.FileName);
    }
}
