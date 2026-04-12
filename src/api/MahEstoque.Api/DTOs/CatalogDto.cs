namespace MahEstoque.Api.DTOs;

public class CatalogProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Size { get; set; }
    public decimal? SalePrice { get; set; }
    public decimal? SalePriceDiscount { get; set; }
    public List<CatalogVariantDto> Variants { get; set; } = new();
    public List<ProductImageDto> Images { get; set; } = new();
}

public class CatalogVariantDto
{
    public Guid Id { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }
    public int Quantity { get; set; }
}

public class CatalogInfoDto
{
    public string TenantName { get; set; } = string.Empty;
    public string? WhatsappNumber { get; set; }
    public List<CatalogProductDto> Products { get; set; } = new();
}
