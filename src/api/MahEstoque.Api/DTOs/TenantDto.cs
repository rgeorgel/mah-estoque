using System.ComponentModel.DataAnnotations;

namespace MahEstoque.Api.DTOs;

public class TenantConfigRequest
{
    [MaxLength(100)]
    public string? Slug { get; set; }

    [MaxLength(30)]
    public string? WhatsappNumber { get; set; }
}

public class TenantConfigDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? WhatsappNumber { get; set; }
}
