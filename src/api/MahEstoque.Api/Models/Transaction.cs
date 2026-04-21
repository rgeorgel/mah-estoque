using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MahEstoque.Api.Models;

public enum TransactionType
{
    Sale,
    Purchase,
    Adjustment
}

public enum PaymentMethod
{
    Dinheiro,
    Pix,
    CartaoDebito,
    CartaoCredito
}

public class Transaction
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    [Required]
    public Guid ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }

    public Guid? VariantId { get; set; }

    [ForeignKey(nameof(VariantId))]
    public ProductVariant? Variant { get; set; }

    public TransactionType Type { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitValue { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalValue { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public PaymentMethod? PaymentMethod { get; set; }

    public int? Installments { get; set; }
}