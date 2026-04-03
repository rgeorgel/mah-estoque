using System.ComponentModel.DataAnnotations;

namespace MahEstoque.Api.DTOs;

public class CreateTransactionRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    public string Type { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    public decimal UnitValue { get; set; }

    public DateTime? CreatedAt { get; set; }
}

public class UpdateTransactionRequest
{
    public int? Quantity { get; set; }
    public decimal? UnitValue { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class TransactionDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSKU { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitValue { get; set; }
    public decimal TotalValue { get; set; }
    public DateTime CreatedAt { get; set; }
}