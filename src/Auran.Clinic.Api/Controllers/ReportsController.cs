using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Models;
using Auran.Clinic.Application.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Route("api/reports")]
[Produces("application/json")]
public sealed class ReportsController(IReportService reportService) : ControllerBase
{
    [HttpGet("operational")]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Reports.View)]
    public async Task<ActionResult<BaseResponse<OperationalReportResponse>>> Operational(
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var report = await reportService.GetOperationalAsync(fromDate, toDate, cancellationToken);
        return Ok(new BaseResponse<OperationalReportResponse>
        {
            Status = true,
            Data = report
        });
    }
}
