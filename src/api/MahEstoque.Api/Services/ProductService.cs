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
            .Include(p => p.Images)
            .AsSplitQuery()
            .Where(p => p.TenantId == tenantId);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(p => p.Category == category);

        var products = await query.OrderBy(p => p.Name).ToListAsync();

        return products.Select(p => MapToListItemDto(p)).ToList();
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id, Guid tenantId)
    {
        var product = await _context.Products
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .AsSplitQuery()
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
            Size = request.Size,
            Description = request.Description,
            SalePrice = request.SalePrice,
            SalePriceDiscount = request.SalePriceDiscount,
            IsVisible = request.IsVisible
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
        // Load only Variants here — no Images Include.
        // Including two collections (Variants + Images) while tracking causes EF Core 10
        // to emit a split query that appends the parent "Id" column to the child result set.
        // The duplicate "Id" column breaks the change-tracker's PK mapping, so the
        // variant UPDATEs end up using the product's Id in the WHERE clause → 0 rows affected.
        // Images are not needed for the update operation; they have their own endpoints.
        var product = await _context.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);
        if (product == null)
            throw new KeyNotFoundException("Produto não encontrado");

        // ── Phase 1: variant changes only ──────────────────────────────────────
        // We intentionally do NOT modify any Product columns here so that the
        // first SaveChangesAsync sends only ProductVariant statements.  Mixing
        // variant UPDATEs with the product UPDATE in a single Npgsql batch caused
        // the product row to report 0 rows affected (DbUpdateConcurrencyException).
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
                    // Use _context.ProductVariants.Add() instead of product.Variants.Add().
                    // ProductVariant.Id has a = Guid.NewGuid() initializer, so the key is
                    // non-empty (non-sentinel) when the object is constructed.  When added
                    // via a navigation collection on an already-tracked parent, EF Core's
                    // relationship fixup sees a "manually assigned" key and tracks the entity
                    // as Modified instead of Added → UPDATE against a non-existent row → 0 rows.
                    // DbSet.Add() always forces the Added state regardless of the key value.
                    _context.ProductVariants.Add(new ProductVariant
                    {
                        ProductId = product.Id,
                        TenantId = tenantId,
                        Size = v.Size,
                        Color = v.Color,
                        SKU = v.SKU,
                        Quantity = v.Quantity
                    });
                }
            }
        }

        // Save variant changes in their own batch (no product changes yet).
        // Harmless no-op if there are no variant changes.
        await _context.SaveChangesAsync();

        // ── Phase 2: product property changes ─────────────────────────────────
        // After the variant save, the Product entity is still Unchanged.
        // Now we modify it so the second SaveChangesAsync sends only the
        // product UPDATE — a clean, single-statement batch that always works.
        if (request.SKU != null) product.SKU = request.SKU;
        if (!string.IsNullOrEmpty(request.Name)) product.Name = request.Name;
        if (request.AcquiredValue.HasValue) product.AcquiredValue = request.AcquiredValue.Value;
        if (request.MinStock.HasValue) product.MinStock = request.MinStock.Value;
        if (request.Category != null) product.Category = request.Category;
        if (request.Supplier != null) product.Supplier = request.Supplier;
        if (request.Size != null) product.Size = request.Size;
        if (request.Description != null) product.Description = request.Description;
        if (request.SalePrice.HasValue) product.SalePrice = request.SalePrice;
        if (request.SalePriceDiscount.HasValue) product.SalePriceDiscount = request.SalePriceDiscount;
        if (request.IsVisible.HasValue) product.IsVisible = request.IsVisible.Value;

        // After phase-1 save, product.Variants reflects the final persisted state.
        if (request.Variants != null)
        {
            product.Quantity = product.Variants.Any() ? 0 : product.Quantity;
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
            .Include(p => p.Images)
            .AsSplitQuery()
            .Where(p => p.TenantId == tenantId)
            .OrderBy(p => p.Quantity)
            .ToListAsync();

        return products
            .Where(p => p.Variants.Any()
                ? p.Variants.Any(v => v.Quantity <= p.MinStock)
                : p.Quantity <= p.MinStock)
            .Select(p => MapToListItemDto(p))
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
        Description = product.Description,
        SalePrice = product.SalePrice,
        SalePriceDiscount = product.SalePriceDiscount,
        IsVisible = product.IsVisible,
        Variants = product.Variants.Select(v => new ProductVariantDto
        {
            Id = v.Id,
            Size = v.Size,
            Color = v.Color,
            SKU = v.SKU,
            Quantity = v.Quantity
        }).ToList(),
        Images = product.Images.OrderBy(i => i.DisplayOrder).Select(i => new ProductImageDto
        {
            Id = i.Id,
            Url = $"/uploads/{i.StoredPath}",
            IsPrimary = i.IsPrimary,
            DisplayOrder = i.DisplayOrder
        }).ToList(),
        CreatedAt = product.CreatedAt,
        UpdatedAt = product.UpdatedAt
    };

    private static ProductListItemDto MapToListItemDto(Product product) => new()
    {
        Id = product.Id,
        SKU = product.SKU,
        Name = product.Name,
        Category = product.Category,
        Supplier = product.Supplier,
        Size = product.Size,
        Description = product.Description,
        AcquiredValue = product.AcquiredValue,
        SalePrice = product.SalePrice,
        SalePriceDiscount = product.SalePriceDiscount,
        IsVisible = product.IsVisible,
        Quantity = product.Variants.Any() ? product.Variants.Sum(v => v.Quantity) : product.Quantity,
        MinStock = product.MinStock,
        Variants = product.Variants.Select(v => new ProductVariantDto
        {
            Id = v.Id,
            Size = v.Size,
            Color = v.Color,
            SKU = v.SKU,
            Quantity = v.Quantity
        }).ToList(),
        Images = product.Images.OrderBy(i => i.DisplayOrder).Select(i => new ProductImageDto
        {
            Id = i.Id,
            Url = $"/uploads/{i.StoredPath}",
            IsPrimary = i.IsPrimary,
            DisplayOrder = i.DisplayOrder
        }).ToList()
    };
}
