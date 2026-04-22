using BarberShop.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarberShop.API.Controllers;

[Authorize]
public class ReportsController(ReportService reportService) : BaseController
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var result = await reportService.GetSummaryAsync(GetUserId());
        return Ok(result);
    }
}