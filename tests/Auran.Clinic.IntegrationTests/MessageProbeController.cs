using Auran.Clinic.Application.Localization;
using Auran.Clinic.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace Auran.Clinic.IntegrationTests;

[ApiController]
[Route("_test/message-probe")]
public sealed class MessageProbeController : ControllerBase
{
    [HttpGet]
    public ActionResult<BaseResponse> Get() => Ok(new BaseResponse { Status = true });

    [HttpPost]
    public ActionResult<BaseResponse> Post() => Ok(new BaseResponse { Status = true });

    [HttpPut]
    public ActionResult<BaseResponse> Put() => Ok(new BaseResponse { Status = true });

    [HttpDelete]
    public ActionResult<BaseResponse> Delete() => Ok(new BaseResponse { Status = true });

    [HttpPost("warning")]
    public ActionResult<BaseResponse> Warning() => Ok(new BaseResponse
    {
        Status = true,
        MessageKey = ApiMessageKeys.Warning
    });
}
