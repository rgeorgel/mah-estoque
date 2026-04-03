using System.ComponentModel.DataAnnotations;

namespace MahEstoque.Api.DTOs;

public class CreateProductRequest
{
    [Required]
    [MaxLength(50)]
    public string SKU { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public decimal AcquiredValue { get; set; }

    public int Quantity { get; set; }

    public int MinStock { get; set; } = 5;

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(100)]
    public string? Supplier { get; set; }

    [MaxLength(50)]
    public string? Size { get; set; }
}

public class UpdateProductRequest
{
    [MaxLength(50)]
    public string? SKU { get; set; }

    [MaxLength(255)]
    public string? Name { get; set; }

    public decimal? AcquiredValue { get; set; }

    public int? Quantity { get; set; }

    public int? MinStock { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(100)]
    public string? Supplier { get; set; }

    [MaxLength(50)]
    public string? Size { get; set; }
}

public class ProductDto
{
    public Guid Id { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal AcquiredValue { get; set; }
    public int Quantity { get; set; }
    public int MinStock { get; set; }
    public string? Category { get; set; }
    public string? Supplier { get; set; }
    public string? Size { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ProductListItemDto
{
    public Guid Id { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Supplier { get; set; }
    public string? Size { get; set; }
    public decimal AcquiredValue { get; set; }
    public int Quantity { get; set; }
    public int MinStock { get; set; }
}