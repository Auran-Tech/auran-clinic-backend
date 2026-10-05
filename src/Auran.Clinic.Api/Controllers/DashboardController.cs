using Auran.Clinic.Application.Authorization;
using Auran.Clinic.Application.Dashboard;
using Auran.Clinic.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auran.Clinic.Api.Controllers;

[ApiController]
[Authorize(Policy = ActorPolicies.Clinic)]
[Route("api/dashboard")]
[Produces("application/json")]
public sealed class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionPolicy.Prefix + Permissions.Dashboard.View)]
    public async Task<ActionResult<BaseResponse<DashboardResponse>>> Get(CancellationToken cancellationToken)
    {
        var result = await dashboardService.GetAsync(cancellationToken);
        return Ok(new BaseResponse<DashboardResponse> { Status = true, Data = result });
    }
}
