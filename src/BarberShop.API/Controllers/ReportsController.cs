using BarberShop.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarberShop.API.Controllers;

[Authorize]
public class ReportsController(ReportService reportService) : BaseController
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] string period = "this-month")
    {
        if (!ReportService.ValidPeriods.Contains(period))
            return BadRequest(new { error = "Período inválido." });

        var result = await reportService.GetSummaryAsync(GetUserId(), period);
        return Ok(result);
    }
}