using System;
using System.ComponentModel.DataAnnotations;

namespace EShop.Shared.Dtos;

public class OrderItemCreateDto
{
    [Required(ErrorMessage = "Ürün Id Zorunludur!")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Bu alan boş bırakılamaz!")]
    [Range(0.01,(double)decimal.MaxValue, ErrorMessage ="Ürün fiyatı 0'dan bütük olmalıdır!")]
    public decimal UnitPrice { get; set; }

    [Required(ErrorMessage = "Bu alan boş bırakılamaz!")]
    [Range(1,100, ErrorMessage = "En Fazla 100 ürün eklenebilir.")]
    public int Quantity { get; set; }

}
