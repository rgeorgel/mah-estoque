using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MahEstoque.Api.DTOs;
using MahEstoque.Api.Services;
using MahEstoque.Api.Extensions;

namespace MahEstoque.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IReportService _reportService;

    public DashboardController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetStats()
    {
        if (!User.IsManagerOrAbove())
            return Forbid();

        var tenantId = User.GetTenantId();
        var stats = await _reportService.GetDashboardStatsAsync(tenantId);
        return Ok(stats);
    }
}

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("sales")]
    public async Task<ActionResult<SalesReportDto>> GetSalesReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        if (!User.IsManagerOrAbove())
            return Forbid();

        var tenantId = User.GetTenantId();
        DateTime? start = null;
        DateTime? end = null;
        
        if (startDate.HasValue)
            start = startDate.Value.Date.ToUniversalTime();
        if (endDate.HasValue)
            end = endDate.Value.Date.AddDays(1).ToUniversalTime();
            
        var report = await _reportService.GetSalesReportAsync(tenantId, start, end);
        return Ok(report);
    }

    [HttpGet("profit")]
    public async Task<ActionResult<ProfitReportDto>> GetProfitReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        if (!User.IsManagerOrAbove())
            return Forbid();

        var tenantId = User.GetTenantId();
        DateTime? start = null;
        DateTime? end = null;
        
        if (startDate.HasValue)
            start = startDate.Value.Date.ToUniversalTime();
        if (endDate.HasValue)
            end = endDate.Value.Date.AddDays(1).ToUniversalTime();
            
        var report = await _reportService.GetProfitReportAsync(tenantId, start, end);
        return Ok(report);
    }

    [HttpGet("stock")]
    public async Task<ActionResult<StockReportDto>> GetStockReport()
    {
        if (!User.IsManagerOrAbove())
            return Forbid();

        var tenantId = User.GetTenantId();
        var report = await _reportService.GetStockReportAsync(tenantId);
        return Ok(report);
    }

    [HttpGet("stock-by-category")]
    public async Task<ActionResult<List<CategoryStockDto>>> GetStockByCategory()
    {
        if (!User.IsManagerOrAbove())
            return Forbid();

        var tenantId = User.GetTenantId();
        var report = await _reportService.GetStockByCategoryAsync(tenantId);
        return Ok(report);
    }

    [HttpGet("stock-by-size")]
    public async Task<ActionResult<List<SizeStockDto>>> GetStockBySize()
    {
        if (!User.IsManagerOrAbove())
            return Forbid();

        var tenantId = User.GetTenantId();
        var report = await _reportService.GetStockBySizeAsync(tenantId);
        return Ok(report);
    }
}