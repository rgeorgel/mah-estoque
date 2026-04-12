using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MahEstoque.Api.Data;
using MahEstoque.Api.DTOs;
using MahEstoque.Api.Extensions;
using MahEstoque.Api.Models;

namespace MahEstoque.Api.Controllers;

[ApiController]
[Route("api/products/{productId}/images")]
[Authorize]
public class ProductImagesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/webp"
    };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB

    public ProductImagesController(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [HttpPost]
    public async Task<ActionResult<ProductImageDto>> Upload(Guid productId, IFormFile file)
    {
        if (!User.IsManagerOrAbove())
            return Forbid();

        var tenantId = User.GetTenantId();

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId && p.TenantId == tenantId);
        if (product == null)
            return NotFound(new { message = "Produto não encontrado" });

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Nenhum arquivo enviado" });

        if (file.Length > MaxFileSizeBytes)
            return BadRequest(new { message = "Arquivo muito grande. Máximo: 5MB" });

        if (!AllowedContentTypes.Contains(file.ContentType))
            return BadRequest(new { message = "Tipo de arquivo não permitido. Use JPEG, PNG ou WebP" });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid()}{ext}";
        var relativePath = Path.Combine(tenantId.ToString(), productId.ToString(), fileName)
            .Replace("\\", "/");

        var uploadRoot = Path.Combine(_env.ContentRootPath, "uploads");
        var uploadDir = Path.Combine(uploadRoot, tenantId.ToString(), productId.ToString());
        Directory.CreateDirectory(uploadDir);

        var fullPath = Path.Combine(uploadDir, fileName);
        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var existingCount = await _context.ProductImages.CountAsync(i => i.ProductId == productId);

        var image = new ProductImage
        {
            ProductId = productId,
            TenantId = tenantId,
            FileName = file.FileName,
            StoredPath = relativePath,
            IsPrimary = existingCount == 0,
            DisplayOrder = existingCount
        };

        _context.ProductImages.Add(image);
        await _context.SaveChangesAsync();

        return Ok(new ProductImageDto
        {
            Id = image.Id,
            Url = $"/uploads/{image.StoredPath}",
            IsPrimary = image.IsPrimary,
            DisplayOrder = image.DisplayOrder
        });
    }

    [HttpPut("{imageId}/set-primary")]
    public async Task<ActionResult> SetPrimary(Guid productId, Guid imageId)
    {
        if (!User.IsManagerOrAbove())
            return Forbid();

        var tenantId = User.GetTenantId();

        var images = await _context.ProductImages
            .Where(i => i.ProductId == productId && i.TenantId == tenantId)
            .ToListAsync();

        if (!images.Any())
            return NotFound();

        foreach (var img in images)
            img.IsPrimary = img.Id == imageId;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{imageId}")]
    public async Task<ActionResult> Delete(Guid productId, Guid imageId)
    {
        if (!User.IsManagerOrAbove())
            return Forbid();

        var tenantId = User.GetTenantId();

        var image = await _context.ProductImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId && i.TenantId == tenantId);

        if (image == null)
            return NotFound();

        var uploadRoot = Path.Combine(_env.ContentRootPath, "uploads");
        var fullPath = Path.Combine(uploadRoot, image.StoredPath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        if (System.IO.File.Exists(fullPath))
            System.IO.File.Delete(fullPath);

        _context.ProductImages.Remove(image);
        await _context.SaveChangesAsync();

        // If deleted image was primary, promote the first remaining
        if (image.IsPrimary)
        {
            var next = await _context.ProductImages
                .Where(i => i.ProductId == productId)
                .OrderBy(i => i.DisplayOrder)
                .FirstOrDefaultAsync();
            if (next != null)
            {
                next.IsPrimary = true;
                await _context.SaveChangesAsync();
            }
        }

        return NoContent();
    }
}
