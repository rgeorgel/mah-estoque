namespace MahEstoque.Api.DTOs;

public class DashboardStatsDto
{
    public int TotalProducts { get; set; }
    public decimal TotalStockValue { get; set; }
    public int SalesToday { get; set; }
    public decimal RevenueToday { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalProfit { get; set; }
    public int LowStockCount { get; set; }
    public List<ProductStockAlertDto> LowStockProducts { get; set; } = new();
    public List<CategoryStockDto> StockByCategory { get; set; } = new();
    public List<DailySalesDto> SalesByDay { get; set; } = new();
    public List<WeeklySalesDto> SalesByWeek { get; set; } = new();
    public List<PaymentMethodStatsDto> SalesByPaymentMethod { get; set; } = new();
}

public class PaymentMethodStatsDto
{
    public string Method { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
}

public class ProductStockAlertDto
{
    public Guid Id { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Size { get; set; }
    public int Quantity { get; set; }
    public int MinStock { get; set; }
}

public class CategoryStockDto
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalValue { get; set; }
}

public class DailySalesDto
{
    public string Date { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
}

public class WeeklySalesDto
{
    public string Week { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
}

public class SalesReportDto
{
    public List<TransactionDto> Transactions { get; set; } = new();
    public decimal TotalRevenue { get; set; }
    public int TotalQuantity { get; set; }
    public decimal AverageValue { get; set; }
}

public class ProfitReportDto
{
    public List<ProductProfitDto> Products { get; set; } = new();
    public decimal TotalProfit { get; set; }
}

public class ProductProfitDto
{
    public Guid ProductId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Size { get; set; }
    public int TotalSold { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal Profit { get; set; }
    public decimal Margin { get; set; }
}

public class StockReportDto
{
    public List<ProductStockItemDto> Products { get; set; } = new();
    public decimal TotalValue { get; set; }
    public int TotalQuantity { get; set; }
}

public class ProductStockItemDto
{
    public Guid Id { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Size { get; set; }
    public int Quantity { get; set; }
    public decimal AcquiredValue { get; set; }
    public decimal TotalValue { get; set; }
}

public class SizeStockDto
{
    public string Size { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalValue { get; set; }
}