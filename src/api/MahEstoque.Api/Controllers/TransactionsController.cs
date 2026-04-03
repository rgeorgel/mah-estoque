using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MahEstoque.Api.DTOs;
using MahEstoque.Api.Services;
using MahEstoque.Api.Extensions;

namespace MahEstoque.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpGet]
    public async Task<ActionResult<List<TransactionDto>>> GetAll(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        if (!User.IsManagerOrAbove())
            return Forbid();

        var tenantId = User.GetTenantId();
        var transactions = await _transactionService.GetAllAsync(tenantId, startDate, endDate);
        return Ok(transactions);
    }

    [HttpPost]
    public async Task<ActionResult<TransactionDto>> Create([FromBody] CreateTransactionRequest request)
    {
        var tenantId = User.GetTenantId();

        try
        {
            var transaction = await _transactionService.CreateAsync(request, tenantId);
            return CreatedAtAction(nameof(GetAll), new { id = transaction.Id }, transaction);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TransactionDto>> Update(Guid id, [FromBody] UpdateTransactionRequest request)
    {
        if (!User.IsManagerOrAbove())
            return Forbid();

        var tenantId = User.GetTenantId();

        try
        {
            var transaction = await _transactionService.UpdateAsync(id, request, tenantId);
            return Ok(transaction);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        if (!User.IsManagerOrAbove())
            return Forbid();

        var tenantId = User.GetTenantId();

        try
        {
            await _transactionService.DeleteAsync(id, tenantId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}