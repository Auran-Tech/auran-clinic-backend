using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Application.Reporting;
using Auran.Clinic.Infrastructure.Reporting;
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
    IValidator<VisitReportQuery> visitReportValidator,
    IValidator<PatientReportQuery> patientReportValidator,
    IValidator<QueueReportQuery> queueReportValidator,
    IValidator<DoctorActivityReportQuery> doctorActivityReportValidator) : ControllerBase
{
    [HttpGet("doctors")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Reports.View)]
    [SwaggerOperation(
        Summary = "Preview doctor activity",
        Description = "Returns per-doctor activity counts for clinic-local visit dates, with current queue independent of dates.",
        OperationId = "Reporting_DoctorActivity",
        Tags = new[] { "Reporting" })]
    public async Task<IActionResult> DoctorActivity(
        [FromQuery] DoctorActivityReportQuery query, CancellationToken cancellationToken)
    {
        var validation = await doctorActivityReportValidator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse { Status = false, Error = "validation_error" });
        try
        {
            var data = await reportingService.GetDoctorActivityReportAsync(query, cancellationToken);
            return Ok(new BaseResponse<DoctorActivityReportResponse> { Status = true, Data = data });
        }
        catch (ReportResultTooLargeException exception)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge,
                new BaseResponse { Status = false, Error = "report_too_large", Message = exception.Message });
        }
    }

    [HttpGet("doctors/export")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Reports.View)]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Reports.Export)]
    [SwaggerOperation(
        Summary = "Export doctor activity CSV",
        Description = "Exports bounded clinic-scoped doctor activity metrics with formula-safe text.",
        OperationId = "Reporting_ExportDoctorActivityCsv",
        Tags = new[] { "Reporting" })]
    [Produces("text/csv")]
    public async Task<IActionResult> ExportDoctorActivity(
        [FromQuery] DoctorActivityReportQuery query, CancellationToken cancellationToken)
    {
        var validation = await doctorActivityReportValidator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse { Status = false, Error = "validation_error" });
        try
        {
            var export = await reportingService.ExportDoctorActivityCsvAsync(query, cancellationToken);
            return File(export.Content, export.ContentType, export.FileName);
        }
        catch (ReportResultTooLargeException exception)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge,
                new BaseResponse { Status = false, Error = "report_too_large", Message = exception.Message });
        }
    }

    [HttpGet("queue")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Reports.View)]
    [SwaggerOperation(
        Summary = "Preview clinic queue report",
        Description = "Returns tenant-scoped queue entries filtered by clinic-local entry date, doctor and workflow status.",
        OperationId = "Reporting_Queue",
        Tags = new[] { "Reporting" })]
    public async Task<IActionResult> QueueReport(
        [FromQuery] QueueReportQuery query, CancellationToken cancellationToken)
    {
        var validation = await queueReportValidator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse { Status = false, Error = "validation_error" });
        try
        {
            var result = await reportingService.GetQueueReportAsync(query, cancellationToken);
            return Ok(new BaseResponse<QueueReportResponse> { Status = true, Data = result });
        }
        catch (ReportResultTooLargeException exception)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge,
                new BaseResponse { Status = false, Error = "report_too_large", Message = exception.Message });
        }
    }

    [HttpGet("queue/export")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Reports.View)]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Reports.Export)]
    [SwaggerOperation(
        Summary = "Export clinic queue report as CSV",
        Description = "Uses the same bounded filters as preview and neutralizes spreadsheet formula injection.",
        OperationId = "Reporting_ExportQueueCsv",
        Tags = new[] { "Reporting" })]
    [Produces("text/csv")]
    public async Task<IActionResult> ExportQueueReport(
        [FromQuery] QueueReportQuery query, CancellationToken cancellationToken)
    {
        var validation = await queueReportValidator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse { Status = false, Error = "validation_error" });
        try
        {
            var export = await reportingService.ExportQueueReportCsvAsync(query, cancellationToken);
            return File(export.Content, export.ContentType, export.FileName);
        }
        catch (ReportResultTooLargeException exception)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge,
                new BaseResponse { Status = false, Error = "report_too_large", Message = exception.Message });
        }
    }

    [HttpGet("patients")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Reports.View)]
    [SwaggerOperation(
        Summary = "Preview clinic patients report",
        Description = "Returns a bounded tenant-scoped patient report filtered by clinic-local creation dates and gender.",
        OperationId = "Reporting_Patients",
        Tags = new[] { "Reporting" })]
    public async Task<IActionResult> Patients(
        [FromQuery] PatientReportQuery query,
        CancellationToken cancellationToken)
    {
        var validation = await patientReportValidator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse { Status = false, Error = "validation_error" });

        try
        {
            var report = await reportingService.GetPatientReportAsync(query, cancellationToken);
            return Ok(new BaseResponse<PatientReportResponse> { Status = true, Data = report });
        }
        catch (ReportResultTooLargeException exception)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge,
                new BaseResponse { Status = false, Error = "report_too_large", Message = exception.Message });
        }
    }

    [HttpGet("patients/export")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Reports.View)]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Reports.Export)]
    [SwaggerOperation(
        Summary = "Export clinic patients report as CSV",
        Description = "Exports bounded tenant-scoped patient rows with spreadsheet-safe text encoding.",
        OperationId = "Reporting_ExportPatientsCsv",
        Tags = new[] { "Reporting" })]
    [Produces("text/csv")]
    public async Task<IActionResult> ExportPatients(
        [FromQuery] PatientReportQuery query,
        CancellationToken cancellationToken)
    {
        var validation = await patientReportValidator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse { Status = false, Error = "validation_error" });

        try
        {
            var export = await reportingService.ExportPatientReportCsvAsync(query, cancellationToken);
            return File(export.Content, export.ContentType, export.FileName);
        }
        catch (ReportResultTooLargeException exception)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge,
                new BaseResponse { Status = false, Error = "report_too_large", Message = exception.Message });
        }
    }

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

        try
        {
            var data = await reportingService.GetVisitReportAsync(query, cancellationToken);
            return Ok(new BaseResponse<VisitReportResponse>
            {
                Status = true,
                Data = data
            });
        }
        catch (ReportResultTooLargeException exception)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge,
                new BaseResponse { Status = false, Error = "report_too_large", Message = exception.Message });
        }
    }

    [HttpGet("visits/export/xlsx")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Reports.View)]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Reports.Export)]
    [SwaggerOperation(
        Summary = "Export visit report as Excel XLSX",
        Description = "Exports tenant-scoped filtered visit records as a true OpenXML workbook, with safe inline text cells and typed UTC dates.",
        OperationId = "Reporting_ExportVisitsXlsx",
        Tags = new[] { "Reporting" })]
    public async Task<IActionResult> ExportVisitsXlsx(
        [FromQuery] VisitReportQuery query,
        CancellationToken cancellationToken)
    {
        var validation = await visitReportValidator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new BaseResponse { Status = false, Error = "validation_error" });

        try
        {
            var report = await reportingService.GetVisitReportAsync(query, cancellationToken);
            var from = report.FromDate?.ToString("yyyyMMdd") ?? "all";
            var to = report.ToDate?.ToString("yyyyMMdd") ?? "all";
            var bytes = VisitReportXlsxWriter.Create(report);
            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"auran-visits-{from}-{to}.xlsx");
        }
        catch (ReportResultTooLargeException exception)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge,
                new BaseResponse { Status = false, Error = "report_too_large", Message = exception.Message });
        }
    }

    [HttpGet("visits/export")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Reports.View)]
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

        try
        {
            var export = await reportingService.ExportVisitReportCsvAsync(query, cancellationToken);
            return File(export.Content, export.ContentType, export.FileName);
        }
        catch (ReportResultTooLargeException exception)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge,
                new BaseResponse { Status = false, Error = "report_too_large", Message = exception.Message });
        }
    }
}
