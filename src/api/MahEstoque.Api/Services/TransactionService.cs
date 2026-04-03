using Microsoft.EntityFrameworkCore;
using MahEstoque.Api.Data;
using MahEstoque.Api.DTOs;
using MahEstoque.Api.Models;

namespace MahEstoque.Api.Services;

public interface ITransactionService
{
    Task<List<TransactionDto>> GetAllAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null);
    Task<TransactionDto> CreateAsync(CreateTransactionRequest request, Guid tenantId);
    Task<TransactionDto> UpdateAsync(Guid id, UpdateTransactionRequest request, Guid tenantId);
    Task DeleteAsync(Guid id, Guid tenantId);
}

public class TransactionService : ITransactionService
{
    private readonly AppDbContext _context;

    public TransactionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<TransactionDto>> GetAllAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Transactions
            .Include(t => t.Product)
            .Where(t => t.TenantId == tenantId);

        if (startDate.HasValue)
            query = query.Where(t => t.CreatedAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(t => t.CreatedAt <= endDate.Value);

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                ProductId = t.ProductId,
                ProductName = t.Product!.Name,
                ProductSKU = t.Product.SKU,
                Type = t.Type.ToString(),
                Quantity = t.Quantity,
                UnitValue = t.UnitValue,
                TotalValue = t.TotalValue,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<TransactionDto> CreateAsync(CreateTransactionRequest request, Guid tenantId)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId && p.TenantId == tenantId);
        if (product == null)
            throw new KeyNotFoundException("Produto não encontrado");

        var totalValue = request.Quantity * request.UnitValue;

        var transactionType = Enum.Parse<TransactionType>(request.Type, true);

        var transaction = new Transaction
        {
            TenantId = tenantId,
            ProductId = request.ProductId,
            Type = transactionType,
            Quantity = request.Quantity,
            UnitValue = request.UnitValue,
            TotalValue = totalValue,
            CreatedAt = request.CreatedAt?.ToUniversalTime() ?? DateTime.UtcNow
        };

        if (transactionType == TransactionType.Sale)
        {
            if (product.Quantity < request.Quantity)
                throw new InvalidOperationException("Estoque insuficiente");
            product.Quantity -= request.Quantity;
        }
        else if (transactionType == TransactionType.Purchase)
        {
            product.Quantity += request.Quantity;
        }
        else if (transactionType == TransactionType.Adjustment)
        {
            product.Quantity = request.Quantity;
        }

        product.UpdatedAt = DateTime.UtcNow;

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        return new TransactionDto
        {
            Id = transaction.Id,
            ProductId = transaction.ProductId,
            ProductName = product.Name,
            ProductSKU = product.SKU,
            Type = transaction.Type.ToString(),
            Quantity = transaction.Quantity,
            UnitValue = transaction.UnitValue,
            TotalValue = transaction.TotalValue,
            CreatedAt = transaction.CreatedAt
        };
    }

    public async Task<TransactionDto> UpdateAsync(Guid id, UpdateTransactionRequest request, Guid tenantId)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Product)
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId);
        
        if (transaction == null)
            throw new KeyNotFoundException("Transação não encontrada");

        var product = transaction.Product;
        if (product == null)
            throw new KeyNotFoundException("Produto não encontrado");

        var oldQuantity = transaction.Quantity;
        var oldType = transaction.Type;

        if (request.Quantity.HasValue)
            transaction.Quantity = request.Quantity.Value;
        
        if (request.UnitValue.HasValue)
            transaction.UnitValue = request.UnitValue.Value;

        transaction.TotalValue = transaction.Quantity * transaction.UnitValue;

        if (request.CreatedAt.HasValue)
            transaction.CreatedAt = request.CreatedAt.Value.ToUniversalTime();

        if (oldType == TransactionType.Sale)
            product.Quantity += oldQuantity;
        else if (oldType == TransactionType.Purchase)
            product.Quantity -= oldQuantity;

        if (transaction.Type == TransactionType.Sale)
        {
            if (product.Quantity < transaction.Quantity)
                throw new InvalidOperationException("Estoque insuficiente");
            product.Quantity -= transaction.Quantity;
        }
        else if (transaction.Type == TransactionType.Purchase)
        {
            product.Quantity += transaction.Quantity;
        }

        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new TransactionDto
        {
            Id = transaction.Id,
            ProductId = transaction.ProductId,
            ProductName = product.Name,
            ProductSKU = product.SKU,
            Type = transaction.Type.ToString(),
            Quantity = transaction.Quantity,
            UnitValue = transaction.UnitValue,
            TotalValue = transaction.TotalValue,
            CreatedAt = transaction.CreatedAt
        };
    }

    public async Task DeleteAsync(Guid id, Guid tenantId)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Product)
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId);
        
        if (transaction == null)
            throw new KeyNotFoundException("Transação não encontrada");

        var product = transaction.Product;
        if (product != null)
        {
            if (transaction.Type == TransactionType.Sale)
                product.Quantity += transaction.Quantity;
            else if (transaction.Type == TransactionType.Purchase)
                product.Quantity -= transaction.Quantity;
            else if (transaction.Type == TransactionType.Adjustment)
                product.Quantity = 0;

            product.UpdatedAt = DateTime.UtcNow;
        }

        _context.Transactions.Remove(transaction);
        await _context.SaveChangesAsync();
    }
}