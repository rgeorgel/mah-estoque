using Microsoft.EntityFrameworkCore;
using MahEstoque.Api.Data;
using MahEstoque.Api.DTOs;
using MahEstoque.Api.Models;

namespace MahEstoque.Api.Services;

public interface IReportService
{
    Task<DashboardStatsDto> GetDashboardStatsAsync(Guid tenantId);
    Task<SalesReportDto> GetSalesReportAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null);
    Task<ProfitReportDto> GetProfitReportAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null);
    Task<StockReportDto> GetStockReportAsync(Guid tenantId);
    Task<List<CategoryStockDto>> GetStockByCategoryAsync(Guid tenantId);
}

public class ReportService : IReportService
{
    private readonly AppDbContext _context;

    public ReportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync(Guid tenantId)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var products = await _context.Products.Where(p => p.TenantId == tenantId).ToListAsync();
        var todaySales = await _context.Transactions
            .Where(t => t.TenantId == tenantId && t.Type == TransactionType.Sale && t.CreatedAt >= today && t.CreatedAt < tomorrow)
            .ToListAsync();

        var allSales = await _context.Transactions
            .Where(t => t.TenantId == tenantId && t.Type == TransactionType.Sale)
            .ToListAsync();

        var totalRevenue = allSales.Sum(t => t.TotalValue);
        var totalCost = allSales.Sum(t => t.Quantity * t.Product!.AcquiredValue);

        var lowStockProducts = products.Where(p => p.Quantity <= p.MinStock).ToList();

        var stockByCategory = products
            .GroupBy(p => string.IsNullOrEmpty(p.Category) ? "Sem categoria" : p.Category)
            .Select(g => new CategoryStockDto
            {
                Category = g.Key,
                Count = g.Sum(p => p.Quantity),
                TotalValue = g.Sum(p => p.Quantity * p.AcquiredValue)
            })
            .ToList();

        var salesByDay = allSales
            .GroupBy(t => t.CreatedAt.Date)
            .OrderByDescending(g => g.Key)
            .Take(7)
            .Select(g => new DailySalesDto
            {
                Date = g.Key.ToString("dd/MM"),
                Quantity = g.Sum(t => t.Quantity),
                Revenue = g.Sum(t => t.TotalValue)
            })
            .Reverse()
            .ToList();

        var threeMonthsAgo = DateTime.UtcNow.AddMonths(-3);
        var salesLast3Months = allSales.Where(t => t.CreatedAt >= threeMonthsAgo).ToList();
        
        var salesByWeek = salesLast3Months
            .GroupBy(t => GetWeekOfYear(t.CreatedAt))
            .OrderBy(g => g.Key)
            .Select(g => new WeeklySalesDto
            {
                Week = g.Key,
                Quantity = g.Sum(t => t.Quantity),
                Revenue = g.Sum(t => t.TotalValue)
            })
            .ToList();

        return new DashboardStatsDto
        {
            TotalProducts = products.Count,
            TotalStockValue = products.Sum(p => p.Quantity * p.AcquiredValue),
            SalesToday = todaySales.Sum(t => t.Quantity),
            RevenueToday = todaySales.Sum(t => t.TotalValue),
            TotalRevenue = totalRevenue,
            TotalProfit = totalRevenue - totalCost,
            LowStockCount = lowStockProducts.Count,
            LowStockProducts = lowStockProducts.Select(p => new ProductStockAlertDto
            {
                Id = p.Id,
                SKU = p.SKU,
                Name = p.Name,
                Quantity = p.Quantity,
                MinStock = p.MinStock
            }).ToList(),
            StockByCategory = stockByCategory,
            SalesByDay = salesByDay,
            SalesByWeek = salesByWeek
        };
    }

    private string GetWeekOfYear(DateTime date)
    {
        var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
        var week = cal.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
        return $"{date.Year}-W{week:D2}";
    }

    public async Task<SalesReportDto> GetSalesReportAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Transactions
            .Include(t => t.Product)
            .Where(t => t.TenantId == tenantId && t.Type == TransactionType.Sale);

        if (startDate.HasValue)
            query = query.Where(t => t.CreatedAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(t => t.CreatedAt <= endDate.Value);

        var transactions = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

        return new SalesReportDto
        {
            Transactions = transactions.Select(t => new TransactionDto
            {
                Id = t.Id,
                ProductId = t.ProductId,
                ProductName = t.Product!.Name,
                ProductSKU = t.Product.SKU,
                Type = t.Type.ToString(),
                Quantity = t.Quantity,
                UnitValue = t.UnitValue,
                TotalValue = t.TotalValue,
                CreatedAt = t.CreatedAt
            }).ToList(),
            TotalRevenue = transactions.Sum(t => t.TotalValue),
            TotalQuantity = transactions.Sum(t => t.Quantity),
            AverageValue = transactions.Any() ? transactions.Average(t => t.TotalValue) : 0
        };
    }

    public async Task<ProfitReportDto> GetProfitReportAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Transactions
            .Include(t => t.Product)
            .Where(t => t.TenantId == tenantId && t.Type == TransactionType.Sale);

        if (startDate.HasValue)
            query = query.Where(t => t.CreatedAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(t => t.CreatedAt <= endDate.Value);

        var transactions = await query.ToListAsync();

        var productIds = transactions.Select(t => t.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var productProfits = transactions
            .GroupBy(t => new { t.ProductId, t.Product!.SKU, t.Product.Name })
            .Select(g =>
            {
                var totalSold = g.Sum(t => t.Quantity);
                var totalRevenue = g.Sum(t => t.TotalValue);
                var product = products[g.Key.ProductId];
                var totalCost = totalSold * product.AcquiredValue;
                var profit = totalRevenue - totalCost;
                return new ProductProfitDto
                {
                    ProductId = g.Key.ProductId,
                    SKU = g.Key.SKU,
                    Name = g.Key.Name,
                    TotalSold = totalSold,
                    TotalRevenue = totalRevenue,
                    TotalCost = totalCost,
                    Profit = profit,
                    Margin = totalRevenue > 0 ? (profit / totalRevenue) * 100 : 0
                };
            })
            .OrderByDescending(p => p.Profit)
            .ToList();

        return new ProfitReportDto
        {
            Products = productProfits,
            TotalProfit = productProfits.Sum(p => p.Profit)
        };
    }

    public async Task<StockReportDto> GetStockReportAsync(Guid tenantId)
    {
        var products = await _context.Products
            .Where(p => p.TenantId == tenantId)
            .OrderBy(p => p.Name)
            .ToListAsync();

        return new StockReportDto
        {
            Products = products.Select(p => new ProductStockItemDto
            {
                Id = p.Id,
                SKU = p.SKU,
                Name = p.Name,
                Category = p.Category,
                Quantity = p.Quantity,
                AcquiredValue = p.AcquiredValue,
                TotalValue = p.Quantity * p.AcquiredValue
            }).ToList(),
            TotalValue = products.Sum(p => p.Quantity * p.AcquiredValue),
            TotalQuantity = products.Sum(p => p.Quantity)
        };
    }

    public async Task<List<CategoryStockDto>> GetStockByCategoryAsync(Guid tenantId)
    {
        return await _context.Products
            .Where(p => p.TenantId == tenantId)
            .GroupBy(p => string.IsNullOrEmpty(p.Category) ? "Sem categoria" : p.Category)
            .Select(g => new CategoryStockDto
            {
                Category = g.Key,
                Count = g.Sum(p => p.Quantity),
                TotalValue = g.Sum(p => p.Quantity * p.AcquiredValue)
            })
            .OrderByDescending(c => c.TotalValue)
            .ToListAsync();
    }
}