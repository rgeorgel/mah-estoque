using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MahEstoque.Api.Models;

public class Product
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    [MaxLength(50)]
    public string? SKU { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal AcquiredValue { get; set; }

    public int Quantity { get; set; }

    public int MinStock { get; set; } = 5;

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(100)]
    public string? Supplier { get; set; }

    [MaxLength(50)]
    public string? Size { get; set; }

    public string? Description { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? SalePrice { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? SalePriceDiscount { get; set; }

    public bool IsVisible { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}