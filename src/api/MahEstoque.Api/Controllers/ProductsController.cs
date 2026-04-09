using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MahEstoque.Api.DTOs;
using MahEstoque.Api.Services;
using MahEstoque.Api.Extensions;

namespace MahEstoque.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductListItemDto>>> GetAll([FromQuery] string? category = null)
    {
        if (!User.IsManagerOrAbove())
            return Forbid();

        var tenantId = User.GetTenantId();
        var products = await _productService.GetAllAsync(tenantId, category);
        return Ok(products);
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<List<ProductListItemDto>>> GetLowStock()
    {
        if (!User.IsManagerOrAbove())
            return Forbid();

        var tenantId = User.GetTenantId();
        var products = await _productService.GetLowStockAsync(tenantId);
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id)
    {
        if (!User.IsManagerOrAbove())
            return Forbid();

        var tenantId = User.GetTenantId();
        var product = await _productService.GetByIdAsync(id, tenantId);
        if (product == null)
            return NotFound(new { message = "Produto não encontrado" });
        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductRequest request)
    {
        if (!User.IsManagerOrAbove())
            return Forbid();

        var tenantId = User.GetTenantId();

        if (!string.IsNullOrEmpty(request.SKU) && !await _productService.IsSkuUniqueAsync(request.SKU, tenantId))
            return BadRequest(new { message = "SKU já está em uso" });

        var product = await _productService.CreateAsync(request, tenantId);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProductDto>> Update(Guid id, [FromBody] UpdateProductRequest request)
    {
        if (!User.IsManagerOrAbove())
            return Forbid();

        var tenantId = User.GetTenantId();

        if (request.SKU != null && !await _productService.IsSkuUniqueAsync(request.SKU, tenantId, id))
            return BadRequest(new { message = "SKU já está em uso" });

        var product = await _productService.UpdateAsync(id, request, tenantId);
        return Ok(product);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        if (!User.IsManagerOrAbove())
            return Forbid();

        var tenantId = User.GetTenantId();
        await _productService.DeleteAsync(id, tenantId);
        return NoContent();
    }
}