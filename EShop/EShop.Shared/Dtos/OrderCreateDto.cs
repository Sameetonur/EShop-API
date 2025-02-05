using System;
using System.Text.Json.Serialization;

namespace EShop.Shared.Dtos;

public class OrderCreateDto
{   
    [JsonIgnore]
    public string? ApplicationUserId { get; set; }
    public ICollection<OrderItemCreateDto> OrderItems { get; set; } = new List<OrderItemCreateDto>();
    public string? Address { get; set; }
    public string? City { get; set; }
    
}
