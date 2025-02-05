using System;
using EShop.Shared.Dtos;
using EShop.Shared.Dtos.ResponseDtos;

namespace EShop.Services.Abstract;

public interface ICartService
{

    Task<ResponseDto<CartDto>> CreateCartAsync(string applicationUserId);

    Task<ResponseDto<CartDto>> GetCartAsync(string applicationUserId);
    
    Task<ResponseDto<CartItemDto>> AddToCartAsync(CartItemCreateDto  cartItemCreateDto);

    Task<ResponseDto<NoContent>> RemoveFromCartAsync(int cartItemId);

    Task<ResponseDto<NoContent>> ClearCartAsync(string applicationUserId);

    Task<ResponseDto<CartItemDto>> ChangeQuantityAsync(CartItemUpdateDto cartItemUpdateDto);


























    // Task<ResponseDto<CartDto>> GetCartByUserIdAsync(string userId); // Sepeti kullanıcıya göre getirir

    // // Sepete ürün ekler
    // Task<ResponseDto<CartDto>> AddToCartAsync(string userId, CartItemCreateDto cartItemCreateDto);

    // // Sepetten ürün çıkarır
    // Task<ResponseDto<NoContent>> DeleteCartAsync(int cartItemId);

    // // Sepetteki bir ürünün miktarını günceller
    // Task<ResponseDto<CartDto>> UpdateCartAsync(CartItemUpdateDto cartItemUpdateDto);


    // // Kullanıcının sepetini tamamen temizler
    // Task<ResponseDto<NoContent>> ClearCartAsync(string userId);

    // // Sepetin toplam tutarını hesaplar
    // Task<ResponseDto<decimal>> GetCartTotalAsync(string userId);






}
