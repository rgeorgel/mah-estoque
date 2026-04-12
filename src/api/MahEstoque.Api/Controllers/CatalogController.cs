using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MahEstoque.Api.Data;
using MahEstoque.Api.DTOs;

namespace MahEstoque.Api.Controllers;

[ApiController]
[Route("api/catalog")]
public class CatalogController : ControllerBase
{
    private readonly AppDbContext _context;

    public CatalogController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<CatalogInfoDto>> GetCatalog(string slug)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Slug == slug);

        if (tenant == null)
            return NotFound(new { message = "Catálogo não encontrado" });

        var products = await _context.Products
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .Where(p => p.TenantId == tenant.Id && p.IsVisible)
            .OrderBy(p => p.Name)
            .ToListAsync();

        var catalogProducts = products.Select(p => new CatalogProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Category = p.Category,
            Size = p.Size,
            SalePrice = p.SalePrice,
            SalePriceDiscount = p.SalePriceDiscount,
            Variants = p.Variants.Select(v => new CatalogVariantDto
            {
                Id = v.Id,
                Size = v.Size,
                Color = v.Color,
                Quantity = v.Quantity
            }).ToList(),
            Images = p.Images.OrderBy(i => i.DisplayOrder).Select(i => new ProductImageDto
            {
                Id = i.Id,
                Url = $"/uploads/{i.StoredPath}",
                IsPrimary = i.IsPrimary,
                DisplayOrder = i.DisplayOrder
            }).ToList()
        }).ToList();

        return Ok(new CatalogInfoDto
        {
            TenantName = tenant.Name,
            WhatsappNumber = tenant.WhatsappNumber,
            Products = catalogProducts
        });
    }
}
