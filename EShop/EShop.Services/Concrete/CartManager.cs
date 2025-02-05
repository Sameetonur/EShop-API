using System;
using AutoMapper;
using EShop.Data.Abstract;
using EShop.Entity.Concrete;
using EShop.Services.Abstract;
using EShop.Shared.Dtos;
using EShop.Shared.Dtos.ResponseDtos;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;

namespace EShop.Services.Concrete;

public class CartManager : ICartService
{

    private readonly IUnitOfWork _unitoFWork;
    private readonly IMapper _mapper;
    private readonly IGenericRepository<Cart> _cartRepository;
    private readonly IGenericRepository<Product> _productRepository;
    private readonly IGenericRepository<CartItem> _cartItemRepository;

    public CartManager(IUnitOfWork unitoFWork, IMapper mapper)
    {
        _unitoFWork = unitoFWork;
        _mapper = mapper;
        _cartRepository = _unitoFWork.GetRepository<Cart>();
        _productRepository = _unitoFWork.GetRepository<Product>();
        _cartItemRepository = _unitoFWork.GetRepository<CartItem>();
    }

    public async Task<ResponseDto<CartItemDto>> AddToCartAsync(CartItemCreateDto cartItemCreateDto)
    {
        try
        {
            var product = await _productRepository.GetAsync(cartItemCreateDto.ProductId);
            if (product == null)
            {
                return ResponseDto<CartItemDto>.Fail("Ürün bulunamadı", StatusCodes.Status404NotFound);
            }
            if (!product.IsActive)
            {
                return ResponseDto<CartItemDto>.Fail("Ürün aktif değil", StatusCodes.Status400BadRequest);

            }

            var cart = await _cartRepository.GetAsync(

                x=>x.Id==cartItemCreateDto.CartId,
                query => query.Include(x=>x.CartItems).ThenInclude(y=>y.Product)
            );

             if (cart == null || cart.CartItems == null)
             {
                    return ResponseDto<CartItemDto>.Fail("Sepet bulunamadı", StatusCodes.Status404NotFound);
             }
                var existsCartItem = cart.CartItems.FirstOrDefault(x => x.ProductId == cartItemCreateDto.ProductId);
                if (existsCartItem != null)
                {
                    existsCartItem.Quantity += cartItemCreateDto.Quantity;
                    _cartItemRepository.Update(existsCartItem);
                     var existsResult = await _unitoFWork.SaveAsync();

                     if (existsResult < 1)
                    {

                    return ResponseDto<CartItemDto>.Fail("Bir Sorun Oluştu!", StatusCodes.Status400BadRequest);
                    }
                     var existcartItemDto = _mapper.Map<CartItemDto>(existsCartItem);
                      return ResponseDto<CartItemDto>.Success(existcartItemDto, StatusCodes.Status200OK);
            }

            var cartItem = new CartItem(
                cartItemCreateDto.CartId,
                cartItemCreateDto.ProductId,
                cartItemCreateDto.Quantity

            );
            // await _cartItemRepository.AddAsync(cartItem);
            // var result = await _unitoFWork.SaveAsync();
            cart.CartItems.Add(cartItem);
             _cartRepository.Update(cart);
             var result = await _unitoFWork.SaveAsync();

            if (result < 1)
            {
                return ResponseDto<CartItemDto>.Fail("Ürün eklenirken bir hata oluştu", StatusCodes.Status500InternalServerError);

            }
            var cartItemDto = _mapper.Map<CartItemDto>(cartItem);
            return ResponseDto<CartItemDto>.Success(cartItemDto, StatusCodes.Status201Created);

        }
        catch (Exception ex)
        {
            return ResponseDto<CartItemDto>.Fail(ex.Message, StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<ResponseDto<CartItemDto>> ChangeQuantityAsync(CartItemUpdateDto cartItemUpdateDto)
    {
        try
        {

            var cartItem = await _cartItemRepository.GetAsync(
                x => x.Id == cartItemUpdateDto.Id,
                query => query.Include(x => x.Product)


            ); //cartItemUpdateDto.Id ile cartItem'ı bul ve güncelle
            //cartItem bulunamazsa hata dön
            if (cartItem == null)
            {
                return ResponseDto<CartItemDto>.Fail("Ürün bulunamadı", StatusCodes.Status404NotFound);
            }


            cartItem.Quantity = cartItemUpdateDto.Quantity; //cartItem'ın miktarını güncelle
            _cartItemRepository.Update(cartItem); //cartItem'ı güncelle
            var result = await _unitoFWork.SaveAsync(); //değişiklikleri kaydet
            if (result < 1)
            {
                return ResponseDto<CartItemDto>.Fail("Ürün miktarı güncellenirken bir hata oluştu", StatusCodes.Status500InternalServerError);
            }
            var CartItemDto = _mapper.Map<CartItemDto>(cartItem); //cartItem'ı CartItemDto'ya dönüştür
            return ResponseDto<CartItemDto>.Success(CartItemDto, StatusCodes.Status200OK);
        }
        catch (Exception ex)
        {
            return ResponseDto<CartItemDto>.Fail(ex.Message, StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<ResponseDto<NoContent>> ClearCartAsync(string applicationUserId)
    {
        try
        {
            var cart = await _cartRepository.GetAsync(
                x => x.ApplicationUserId == applicationUserId,
                query => query.Include(x => x.CartItems)
            );
            if (cart == null)
            {
                return ResponseDto<NoContent>.Fail("Sepet bulunamadı", StatusCodes.Status404NotFound);

            }
            cart.CartItems?.Clear();
            _cartRepository.Update(cart);
            var result = await _unitoFWork.SaveAsync();
            if (result < 1)
            {
                return ResponseDto<NoContent>.Fail("Sepet temizlenirken bir hata oluştu", StatusCodes.Status500InternalServerError);
            }
            return ResponseDto<NoContent>.Success(StatusCodes.Status204NoContent);
        }
        catch (Exception ex)
        {
            return ResponseDto<NoContent>.Fail(ex.Message, StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<ResponseDto<CartDto>> CreateCartAsync(string applicationUserId)
    {
        try
        {
            if (string.IsNullOrEmpty(applicationUserId))//isnullorempty hem null mu hem boşmuyu tekte kontrol etmemi sağlıyor.
            {
                return ResponseDto<CartDto>.Fail("Kullanıcı id'si boş olamaz", StatusCodes.Status400BadRequest);
            }
            var existscart = await _cartRepository.GetAsync(x => x.ApplicationUserId == applicationUserId);
            if (existscart != null)
            {
                var existcartDto = _mapper.Map<CartDto>(existscart);
                return ResponseDto<CartDto>.Success(existcartDto, StatusCodes.Status400BadRequest);
            }
            var cart = new Cart(applicationUserId); //cart yoksa yeni cart oluştur
            await _cartRepository.AddAsync(cart); //cart'ı ekle
            var result = await _unitoFWork.SaveAsync(); //değişiklikleri kaydet
            if (result < 1)
            {
                return ResponseDto<CartDto>.Fail("Sepet oluşturulurken bir hata oluştu", StatusCodes.Status500InternalServerError);
            }
            var cartDto = _mapper.Map<CartDto>(cart); //cart'ı CartDto'ya dönüştür
            return ResponseDto<CartDto>.Success(cartDto, StatusCodes.Status201Created);
        }
        catch (Exception ex)
        {
            return ResponseDto<CartDto>.Fail(ex.Message, StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<ResponseDto<NoContent>> RemoveFromCartAsync(int cartItemId)
    {
        try
        {
            var cartItem = await _cartItemRepository.GetAsync(cartItemId);
            if (cartItem==null)
            {
                return ResponseDto<NoContent>.Fail("İlgili ürün sepette bulunamadığı için silinmedi",StatusCodes.Status404NotFound);
                
            }
            _cartItemRepository.Delete(cartItem);
            var result = await _unitoFWork.SaveAsync();
            if (result<1)
            {
                return ResponseDto<NoContent>.Fail("Bir Sorun Oluştu", StatusCodes.Status404NotFound);

            }
            return ResponseDto<NoContent>.Success( StatusCodes.Status200OK);
        }
        catch (Exception ex)
        {
            return ResponseDto<NoContent>.Fail(ex.Message, StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<ResponseDto<CartDto>> GetCartAsync(string applicationUserId)
    {
        try
        {
            if (string.IsNullOrEmpty(applicationUserId))//is null or empty hem null mu hem boşmuyu tekte kontrol etmemi sağlıyor.
            {
                return ResponseDto<CartDto>.Fail("Kullanıcı id'si boş olamaz", StatusCodes.Status400BadRequest);
            }
            var cart = await _cartRepository.GetAsync(
                x=>x.ApplicationUserId==applicationUserId,
                query =>  query.Include(x=>x.CartItems).ThenInclude(y=>y.Product)
            );
            if (cart == null)
            {
                return ResponseDto<CartDto>.Fail("Kullanıcıya ait sepet bulunamadı",StatusCodes.Status404NotFound);
                
            }
            var cartDto = _mapper.Map<CartDto>(cart);
            return ResponseDto<CartDto>.Success(cartDto, StatusCodes.Status200OK);
        }
        catch (Exception ex)
        {
            return ResponseDto<CartDto>.Fail(ex.Message, StatusCodes.Status500InternalServerError);
        }
    }
}
