using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MahEstoque.Api.Data;
using MahEstoque.Api.DTOs;
using MahEstoque.Api.Extensions;

namespace MahEstoque.Api.Controllers;

[ApiController]
[Route("api/tenant")]
[Authorize]
public class TenantController : ControllerBase
{
    private readonly AppDbContext _context;

    public TenantController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("config")]
    public async Task<ActionResult<TenantConfigDto>> GetConfig()
    {
        var tenantId = User.GetTenantId();
        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null)
            return NotFound();

        return Ok(new TenantConfigDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Slug = tenant.Slug,
            WhatsappNumber = tenant.WhatsappNumber
        });
    }

    [HttpPut("config")]
    public async Task<ActionResult<TenantConfigDto>> UpdateConfig([FromBody] TenantConfigRequest request)
    {
        if (!User.IsAdmin())
            return Forbid();

        var tenantId = User.GetTenantId();
        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null)
            return NotFound();

        if (request.Slug != null)
        {
            var slugTaken = await _context.Tenants
                .AnyAsync(t => t.Slug == request.Slug && t.Id != tenantId);
            if (slugTaken)
                return BadRequest(new { message = "Este slug já está em uso" });
            tenant.Slug = request.Slug;
        }

        if (request.WhatsappNumber != null)
            tenant.WhatsappNumber = request.WhatsappNumber;

        await _context.SaveChangesAsync();

        return Ok(new TenantConfigDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Slug = tenant.Slug,
            WhatsappNumber = tenant.WhatsappNumber
        });
    }
}
