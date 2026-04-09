using Microsoft.EntityFrameworkCore;
using MahEstoque.Api.Data;
using MahEstoque.Api.DTOs;
using MahEstoque.Api.Models;

namespace MahEstoque.Api.Services;

public interface IProductService
{
    Task<List<ProductListItemDto>> GetAllAsync(Guid tenantId, string? category = null);
    Task<ProductDto?> GetByIdAsync(Guid id, Guid tenantId);
    Task<ProductDto> CreateAsync(CreateProductRequest request, Guid tenantId);
    Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request, Guid tenantId);
    Task DeleteAsync(Guid id, Guid tenantId);
    Task<List<ProductListItemDto>> GetLowStockAsync(Guid tenantId);
    Task<bool> IsSkuUniqueAsync(string sku, Guid tenantId, Guid? excludeId = null);
}

public class ProductService : IProductService
{
    private readonly AppDbContext _context;

    public ProductService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductListItemDto>> GetAllAsync(Guid tenantId, string? category = null)
    {
        var query = _context.Products
            .Include(p => p.Variants)
            .Where(p => p.TenantId == tenantId);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(p => p.Category == category);

        var products = await query.OrderBy(p => p.Name).ToListAsync();

        return products.Select(p => new ProductListItemDto
        {
            Id = p.Id,
            SKU = p.SKU,
            Name = p.Name,
            Category = p.Category,
            Supplier = p.Supplier,
            Size = p.Size,
            AcquiredValue = p.AcquiredValue,
            Quantity = p.Variants.Any() ? p.Variants.Sum(v => v.Quantity) : p.Quantity,
            MinStock = p.MinStock,
            Variants = p.Variants.Select(v => new ProductVariantDto
            {
                Id = v.Id,
                Size = v.Size,
                Color = v.Color,
                SKU = v.SKU,
                Quantity = v.Quantity
            }).ToList()
        }).ToList();
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id, Guid tenantId)
    {
        var product = await _context.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);
        return product == null ? null : MapToDto(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, Guid tenantId)
    {
        var product = new Product
        {
            TenantId = tenantId,
            SKU = request.SKU,
            Name = request.Name,
            AcquiredValue = request.AcquiredValue,
            Quantity = request.Variants?.Count > 0 ? 0 : request.Quantity,
            MinStock = request.MinStock,
            Category = request.Category,
            Supplier = request.Supplier,
            Size = request.Size
        };

        if (request.Variants?.Count > 0)
        {
            foreach (var v in request.Variants)
            {
                product.Variants.Add(new ProductVariant
                {
                    TenantId = tenantId,
                    Size = v.Size,
                    Color = v.Color,
                    SKU = v.SKU,
                    Quantity = v.Quantity
                });
            }
        }

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return MapToDto(product);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request, Guid tenantId)
    {
        var product = await _context.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);
        if (product == null)
            throw new KeyNotFoundException("Produto não encontrado");

        if (request.SKU != null) product.SKU = request.SKU;
        if (!string.IsNullOrEmpty(request.Name)) product.Name = request.Name;
        if (request.AcquiredValue.HasValue) product.AcquiredValue = request.AcquiredValue.Value;
        if (request.MinStock.HasValue) product.MinStock = request.MinStock.Value;
        if (request.Category != null) product.Category = request.Category;
        if (request.Supplier != null) product.Supplier = request.Supplier;
        if (request.Size != null) product.Size = request.Size;

        if (request.Variants != null)
        {
            var incomingIds = request.Variants
                .Where(v => v.Id.HasValue)
                .Select(v => v.Id!.Value)
                .ToHashSet();

            // Remove variants not in the incoming list (only if they have no transactions)
            foreach (var existing in product.Variants.ToList())
            {
                if (!incomingIds.Contains(existing.Id))
                {
                    var hasTransactions = await _context.Transactions.AnyAsync(t => t.VariantId == existing.Id);
                    if (!hasTransactions)
                        _context.ProductVariants.Remove(existing);
                }
            }

            // Update existing or create new
            foreach (var v in request.Variants)
            {
                if (v.Id.HasValue)
                {
                    var existing = product.Variants.FirstOrDefault(pv => pv.Id == v.Id.Value);
                    if (existing != null)
                    {
                        existing.Size = v.Size;
                        existing.Color = v.Color;
                        existing.SKU = v.SKU;
                        existing.Quantity = v.Quantity;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    product.Variants.Add(new ProductVariant
                    {
                        TenantId = tenantId,
                        Size = v.Size,
                        Color = v.Color,
                        SKU = v.SKU,
                        Quantity = v.Quantity
                    });
                }
            }

            // If product now has variants, zero out the product-level quantity
            if (product.Variants.Any())
                product.Quantity = 0;
        }
        else if (request.Quantity.HasValue && !product.Variants.Any())
        {
            product.Quantity = request.Quantity.Value;
        }

        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToDto(product);
    }

    public async Task DeleteAsync(Guid id, Guid tenantId)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);
        if (product == null)
            throw new KeyNotFoundException("Produto não encontrado");

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
    }

    public async Task<List<ProductListItemDto>> GetLowStockAsync(Guid tenantId)
    {
        var products = await _context.Products
            .Include(p => p.Variants)
            .Where(p => p.TenantId == tenantId)
            .OrderBy(p => p.Quantity)
            .ToListAsync();

        return products
            .Where(p => p.Variants.Any()
                ? p.Variants.Any(v => v.Quantity <= p.MinStock)
                : p.Quantity <= p.MinStock)
            .Select(p => new ProductListItemDto
            {
                Id = p.Id,
                SKU = p.SKU,
                Name = p.Name,
                Category = p.Category,
                Supplier = p.Supplier,
                Size = p.Size,
                AcquiredValue = p.AcquiredValue,
                Quantity = p.Variants.Any() ? p.Variants.Sum(v => v.Quantity) : p.Quantity,
                MinStock = p.MinStock,
                Variants = p.Variants.Select(v => new ProductVariantDto
                {
                    Id = v.Id,
                    Size = v.Size,
                    Color = v.Color,
                    SKU = v.SKU,
                    Quantity = v.Quantity
                }).ToList()
            })
            .ToList();
    }

    public async Task<bool> IsSkuUniqueAsync(string sku, Guid tenantId, Guid? excludeId = null)
    {
        var query = _context.Products.Where(p => p.TenantId == tenantId && p.SKU == sku);
        if (excludeId.HasValue)
            query = query.Where(p => p.Id != excludeId.Value);
        return !await query.AnyAsync();
    }

    private static ProductDto MapToDto(Product product) => new()
    {
        Id = product.Id,
        SKU = product.SKU,
        Name = product.Name,
        AcquiredValue = product.AcquiredValue,
        Quantity = product.Variants.Any() ? product.Variants.Sum(v => v.Quantity) : product.Quantity,
        MinStock = product.MinStock,
        Category = product.Category,
        Supplier = product.Supplier,
        Size = product.Size,
        Variants = product.Variants.Select(v => new ProductVariantDto
        {
            Id = v.Id,
            Size = v.Size,
            Color = v.Color,
            SKU = v.SKU,
            Quantity = v.Quantity
        }).ToList(),
        CreatedAt = product.CreatedAt,
        UpdatedAt = product.UpdatedAt
    };
}
