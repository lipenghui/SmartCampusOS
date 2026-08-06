using IdentityService.Application.DTOs.Audit;
using IdentityService.Application.UseCases.Audit;
using Microsoft.AspNetCore.Mvc;
using SmartCampusOS.SharedKernel.Results;

namespace IdentityService.Api.Controllers;

/// <summary>
/// 审计日志接口（LLD §3.2 GET /api/v1/audit-logs / Common-04）。
/// </summary>
[ApiController]
[Route("api/v1/audit-logs")]
public sealed class AuditLogsController(AuditService auditService) : ControllerBase
{
    /// <summary>审计日志检索（用户/动作/目标类型/时间过滤，分页）。</summary>
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] AuditQuery query, CancellationToken ct)
    {
        var result = await auditService.SearchAsync(query, ct);
        return Ok(ApiResponse<PagedResult<AuditLogDto>>.Ok(result));
    }
}
