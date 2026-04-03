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
        var query = _context.Products.Where(p => p.TenantId == tenantId);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(p => p.Category == category);

        return await query
            .OrderBy(p => p.Name)
            .Select(p => new ProductListItemDto
            {
                Id = p.Id,
                SKU = p.SKU,
                Name = p.Name,
                Category = p.Category,
                Supplier = p.Supplier,
                Size = p.Size,
                AcquiredValue = p.AcquiredValue,
                Quantity = p.Quantity,
                MinStock = p.MinStock
            })
            .ToListAsync();
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id, Guid tenantId)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);
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
            Quantity = request.Quantity,
            MinStock = request.MinStock,
            Category = request.Category,
            Supplier = request.Supplier,
            Size = request.Size
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return MapToDto(product);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request, Guid tenantId)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);
        if (product == null)
            throw new KeyNotFoundException("Produto não encontrado");

        if (!string.IsNullOrEmpty(request.SKU)) product.SKU = request.SKU;
        if (!string.IsNullOrEmpty(request.Name)) product.Name = request.Name;
        if (request.AcquiredValue.HasValue) product.AcquiredValue = request.AcquiredValue.Value;
        if (request.Quantity.HasValue) product.Quantity = request.Quantity.Value;
        if (request.MinStock.HasValue) product.MinStock = request.MinStock.Value;
        product.Category = request.Category ?? product.Category;
        product.Supplier = request.Supplier ?? product.Supplier;
        product.Size = request.Size ?? product.Size;
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
        return await _context.Products
            .Where(p => p.TenantId == tenantId && p.Quantity <= p.MinStock)
            .OrderBy(p => p.Quantity)
            .Select(p => new ProductListItemDto
            {
                Id = p.Id,
                SKU = p.SKU,
                Name = p.Name,
                Category = p.Category,
                Supplier = p.Supplier,
                Size = p.Size,
                AcquiredValue = p.AcquiredValue,
                Quantity = p.Quantity,
                MinStock = p.MinStock
            })
            .ToListAsync();
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
        Quantity = product.Quantity,
        MinStock = product.MinStock,
        Category = product.Category,
        Supplier = product.Supplier,
        Size = product.Size,
        CreatedAt = product.CreatedAt,
        UpdatedAt = product.UpdatedAt
    };
}