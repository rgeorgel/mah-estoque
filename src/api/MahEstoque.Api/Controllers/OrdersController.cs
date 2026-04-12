using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MahEstoque.Api.Data;
using MahEstoque.Api.DTOs;
using MahEstoque.Api.Extensions;
using MahEstoque.Api.Models;

namespace MahEstoque.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _context;

    public OrdersController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Public endpoint called by the catalog page before opening WhatsApp.
    /// Creates a Pending order and returns its reference code.
    /// </summary>
    [HttpPost("{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateOrder(string slug, [FromBody] CreateOrderRequest request)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Slug == slug);
        if (tenant == null) return NotFound(new { message = "Catálogo não encontrado" });

        if (request.Items == null || request.Items.Count == 0)
            return BadRequest(new { message = "Pedido sem itens." });

        var order = new Order
        {
            TenantId = tenant.Id,
            CustomerName = request.CustomerName?.Trim(),
            CustomerPhone = request.CustomerPhone?.Trim(),
            TotalValue = request.Items.Sum(i => i.UnitPrice * i.Quantity)
        };

        foreach (var item in request.Items)
        {
            order.Items.Add(new OrderItem
            {
                ProductId = item.ProductId,
                VariantId = item.VariantId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            id = order.Id,
            orderRef = OrderRef(order.Id)
        });
    }

    /// <summary>Admin/Manager: list all orders for the tenant, optionally filtered by status.</summary>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<OrderDto>>> GetOrders([FromQuery] string? status = null)
    {
        if (!User.IsManagerOrAbove()) return Forbid();

        var tenantId = User.GetTenantId();

        var query = _context.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Items).ThenInclude(i => i.Variant)
            .Where(o => o.TenantId == tenantId)
            .AsSplitQuery();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, true, out var parsed))
            query = query.Where(o => o.Status == parsed);

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
        return Ok(orders.Select(MapToDto).ToList());
    }

    /// <summary>Admin/Manager: update order status.</summary>
    [HttpPut("{id}/status")]
    [Authorize]
    public async Task<ActionResult<OrderDto>> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusRequest request)
    {
        if (!User.IsManagerOrAbove()) return Forbid();

        var tenantId = User.GetTenantId();

        if (!Enum.TryParse<OrderStatus>(request.Status, true, out var newStatus))
            return BadRequest(new { message = "Status inválido. Use: Pending, Confirmed ou Cancelled." });

        var order = await _context.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Items).ThenInclude(i => i.Variant)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId);

        if (order == null) return NotFound();

        order.Status = newStatus;
        await _context.SaveChangesAsync();

        return Ok(MapToDto(order));
    }

    private static string OrderRef(Guid id) => id.ToString("N")[..8].ToUpper();

    private static OrderDto MapToDto(Order order) => new()
    {
        Id = order.Id,
        OrderRef = OrderRef(order.Id),
        Status = order.Status.ToString(),
        CustomerName = order.CustomerName,
        CustomerPhone = order.CustomerPhone,
        TotalValue = order.TotalValue,
        CreatedAt = order.CreatedAt,
        Items = order.Items.Select(i => new OrderItemDto
        {
            ProductId = i.ProductId,
            ProductName = i.Product?.Name ?? "(produto removido)",
            VariantId = i.VariantId,
            VariantLabel = i.Variant != null
                ? string.Join(" / ", new[] { i.Variant.Size, i.Variant.Color }.Where(x => x != null))
                : null,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList()
    };
}
