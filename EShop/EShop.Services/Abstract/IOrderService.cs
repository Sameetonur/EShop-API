using System;
using EShop.Shared.ComplexTypes;
using EShop.Shared.Dtos;
using EShop.Shared.Dtos.ResponseDtos;

namespace EShop.Services.Abstract;

public interface IOrderService
{
   Task<ResponseDto<OrderDto>> GetAsync(int id);
    Task<ResponseDto<IEnumerable<OrderDto>>> GetAllAsync();

    Task<ResponseDto<IEnumerable<OrderDto>>> GetAllAsync(OrderStatus orderStatus , string? applicationUserId = null);

    Task<ResponseDto<OrderDto>> GetAllAsync(string applicationUserId);

    Task<ResponseDto<IEnumerable<OrderDto>>> GetAllAsync(DateTime startDate, DateTime endDate);

    Task<ResponseDto<OrderDto>> AddAsync(OrderCreateDto  orderCreateDto);

    Task<ResponseDto<OrderDto>> UpdateOrderStatusAsync(int id, OrderStatus status);

    Task<ResponseDto<NoContent>> CancelOrderAsync(int id); 














































    // // Siparişi ID'ye göre getirir
    // Task<ResponseDto<OrderDto>> GetAsync(int id);

    // // Tüm siparişleri getirir
    // Task<ResponseDto<IEnumerable<OrderDto>>> GetAllAsync();

    // // Sipariş ekler
    // Task<ResponseDto<OrderDto>> AddAsync(OrderCreateDto orderCreateDto);

    // // Siparişi siler (Soft delete)
    // Task<ResponseDto<NoContent>> SoftDeleteAsync(int id);

    // // Siparişi kalıcı olarak siler (Hard delete)
    // Task<ResponseDto<NoContent>> HardDeleteAsync(int id);

    // // Sipariş sayısını getirir
    // Task<ResponseDto<int>> CountAsync();

    // // Sepeti siparişe dönüştürür (Checkout)
    // Task<ResponseDto<OrderDto>> CheckoutAsync(string userId);

    // // Sipariş sayısını filtreye göre getirir (Örn: aktif, pasif)
    // Task<ResponseDto<IEnumerable<OrderDto>>> GetByStatusAsync(OrderStatusType? status);

    // // Siparişin durumunu günceller (Örn: gönderildi, onaylandı vb.)
    // Task<ResponseDto<OrderDto>> UpdateOrderStatusAsync(int id, OrderStatusType status);

    // // Siparişi kullanıcıya göre getirir
    // Task<ResponseDto<IEnumerable<OrderDto>>> GetUserOrdersAsync(string userId);


}
