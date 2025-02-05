using System;
using System.Security.Cryptography.X509Certificates;
using AutoMapper;
using Azure;
using EShop.Data.Abstract;
using EShop.Entity.Concrete;
using EShop.Services.Abstract;
using EShop.Shared.ComplexTypes;
using EShop.Shared.Dtos;
using EShop.Shared.Dtos.ResponseDtos;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EShop.Services.Concrete;

public class OrderManager : IOrderService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICartService _cartManager;
    private readonly IGenericRepository<Order> _orderRepository;
    private readonly IGenericRepository<Product> _productRepository;

    public OrderManager(IUnitOfWork uow, IMapper mapper, ICartService cartManager)
    {
        _uow = uow;
        _mapper = mapper;
        _cartManager = cartManager;
        _orderRepository = _uow.GetRepository<Order>();
        _productRepository = _uow.GetRepository<Product>();
    }

    public async Task<ResponseDto<OrderDto>> AddAsync(OrderCreateDto orderCreateDto)
    {
        try
        {

            foreach (var orderItem in orderCreateDto.OrderItems)

            {
                var ExistsProduct = await _productRepository.GetAsync(x => x.Id == orderItem.ProductId);
                if (ExistsProduct == null)
                {
                    return ResponseDto<OrderDto>.Fail($"{orderItem.ProductId}'li ürün bulunamadığı için işlem iptal edilmiştir", StatusCodes.Status404NotFound);
                }
                if (!ExistsProduct.IsActive)
                {
                    return ResponseDto<OrderDto>.Fail($"{orderItem.ProductId}'li ürün aktif olmadığı için işlem iptal edilmiştir", StatusCodes.Status400BadRequest);
                }

            }


            var order = _mapper.Map<Order>(orderCreateDto);

            // Order order = new(orderCreateDto.ApplicationUserId, orderCreateDto.Address,orderCreateDto.City)
            // {
            //         OrderItems=orderCreateDto.OrderItems
            //         .Select(x=> new OrderItem(x.ProductId,x.UnitPrice,x.Quantity)).ToList()
            // };
            //fake ödeme operasyonunu ekleyeceğiz.
            await _orderRepository.AddAsync(order);
            await _uow.SaveAsync();
            // orderıtemler ile ilgili ekstra bir işlem yapmayıp, bunu izleyip sonuçlarını değerlendireceğiz. Gerekdirse buraya gelip gereken işlemleri yapacağız.
            await _cartManager.ClearCartAsync(orderCreateDto.ApplicationUserId);
            var OrderDto = _mapper.Map<OrderDto>(order);
            return ResponseDto<OrderDto>.Success(OrderDto, StatusCodes.Status201Created);

        }
        catch (System.Exception ex)
        {
            return ResponseDto<OrderDto>.Fail(ex.Message, StatusCodes.Status500InternalServerError);

        }
    }

    public async Task<ResponseDto<NoContent>> CancelOrderAsync(int id)
    {
        try
        {
            var order = await _orderRepository.GetAsync(id);
            if (order == null)
            {
                return ResponseDto<NoContent>.Fail("Sipariş bulunamadı", StatusCodes.Status404NotFound);
            }
            order.IsDeleted = true;
            order.IsActive=false;
            _orderRepository.Update(order);
            var result = await _uow.SaveAsync();
            if (result > 1)
            {
                return ResponseDto<NoContent>.Fail("İşlem tamamlanamadı!", StatusCodes.Status500InternalServerError);

            }
            return ResponseDto<NoContent>.Success(StatusCodes.Status204NoContent);
        }
        catch (System.Exception ex)
        {
            return ResponseDto<NoContent>.Fail(ex.Message, StatusCodes.Status500InternalServerError);

        }
    }

    public async Task<ResponseDto<IEnumerable<OrderDto>>> GetAllAsync()
    {
        try
        {
            var orders = await _orderRepository.GetAllAsync(
               orderBy: x => x.OrderByDescending(x => x.CreateDate),
               includes: query => query.Include(x => x.ApplicationUser).Include(x => x.OrderItems).ThenInclude(y=>y.Product)
               );
            if (orders == null || !orders.Any())
            {
                return ResponseDto<IEnumerable<OrderDto>>.Fail("Herhangi bir spariş bilgisi bulunamadı!", StatusCodes.Status404NotFound);
            }
            var OrderDtos = _mapper.Map<IEnumerable<OrderDto>>(orders);
            return ResponseDto<IEnumerable<OrderDto>>.Success(OrderDtos, StatusCodes.Status200OK);

        }
        catch (System.Exception ex)
        {
            return ResponseDto<IEnumerable<OrderDto>>.Fail(ex.Message, StatusCodes.Status500InternalServerError);

        }
    }

    public async Task<ResponseDto<IEnumerable<OrderDto>>> GetAllAsync(OrderStatus orderStatus, string? applicationUserId = null)
    {
        try        
        {
            var orders =
               applicationUserId == null ?
               await _orderRepository.GetAllAsync(
               predicate: x => x.OrderStatus == orderStatus,
               orderBy: x => x.OrderByDescending(x => x.CreateDate),
               includes: query => query
                               .Include(x => x.ApplicationUser)
                               .Include(x => x.OrderItems)
                               .ThenInclude(y=>y.Product)) :
               await _orderRepository.GetAllAsync(
                   predicate: x => x.OrderStatus == orderStatus && x.ApplicationUserId == applicationUserId,
                   orderBy: x => x.OrderByDescending(x => x.CreateDate),
                   includes: query => query
                                   .Include(x => x.ApplicationUser)
                                   .Include(x => x.OrderItems)
                                   .ThenInclude(y=>y.Product)
               );
            if (orders == null || !orders.Any())
            {
                return ResponseDto<IEnumerable<OrderDto>>.Fail("Herhangi bir spariş bilgisi bulunamadı!", StatusCodes.Status404NotFound);
            }
            var OrderDtos = _mapper.Map<IEnumerable<OrderDto>>(orders);
            return ResponseDto<IEnumerable<OrderDto>>.Success(OrderDtos, StatusCodes.Status200OK);

        }
        catch (System.Exception ex)
        {
            return ResponseDto<IEnumerable<OrderDto>>.Fail(ex.Message, StatusCodes.Status500InternalServerError);

        }
    }

    public async Task<ResponseDto<OrderDto>> GetAllAsync(string applicationUserId)
    {
        try
        {
            // (string.IsNullOrEmpty(applicationUserId) || x.ApplicationUserId == applicationUserId),
            var orders = await _orderRepository.GetAllAsync(
                predicate: x => x.ApplicationUserId == applicationUserId,
                orderBy: x => x.OrderByDescending(x => x.CreateDate),
                includes: query => query.Include(x => x.ApplicationUser).Include(x => x.OrderItems).ThenInclude(y=>y.Product)
            );
            if (orders == null || !orders.Any())
            {
                return ResponseDto<OrderDto>.Fail("Herhangi bir spariş bilgisi bulunamadı!", StatusCodes.Status404NotFound);
            }
            var OrderDtos = _mapper.Map<OrderDto>(orders);
            return ResponseDto<OrderDto>.Success(OrderDtos, StatusCodes.Status200OK);

        }
        catch (System.Exception ex)
        {
            return ResponseDto<OrderDto>.Fail(ex.Message, StatusCodes.Status500InternalServerError);

        }
    }

    public async Task<ResponseDto<IEnumerable<OrderDto>>> GetAllAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            startDate = startDate == null ? new DateTime(1900, 1, 1) : startDate;
            // (string.IsNullOrEmpty(applicationUserId) || x.ApplicationUserId == applicationUserId),
            var orders = await _orderRepository.GetAllAsync(
                predicate: x => x.CreateDate >= startDate && x.CreateDate <= endDate,
                includes: query =>
                            query.Include(x => x.ApplicationUser)
                                .Include(x => x.OrderItems).ThenInclude(y=>y.Product)
            );
            if (orders == null || !orders.Any())
            {
                return ResponseDto<IEnumerable<OrderDto>>.Fail("Herhangi bir spariş bilgisi bulunamadı!", StatusCodes.Status404NotFound);
            }
            var OrderDtos = _mapper.Map<IEnumerable<OrderDto>>(orders);
            return ResponseDto<IEnumerable<OrderDto>>.Success(OrderDtos, StatusCodes.Status200OK);

        }
        catch (System.Exception ex)
        {
            return ResponseDto<IEnumerable<OrderDto>>.Fail(ex.Message, StatusCodes.Status500InternalServerError);

        }
    }

    public async Task<ResponseDto<OrderDto>> GetAsync(int id)
    {
        try
        {
            var order = await _orderRepository.GetAsync(
            predicate: x => x.Id == id,
            includes: query => query.Include(X => X.ApplicationUser)
                                    .Include(x => x.OrderItems)
                                    .ThenInclude(y => y.Product)
        );
            if (order == null)
            {
                return ResponseDto<OrderDto>.Fail("İlgili sipariş bulunamadı", StatusCodes.Status404NotFound);
            }
            var orderDto = _mapper.Map<OrderDto>(order);
            return ResponseDto<OrderDto>.Success(orderDto, StatusCodes.Status200OK);
        }
        catch (System.Exception ex)
        {
            return ResponseDto<OrderDto>.Fail(ex.Message, StatusCodes.Status500InternalServerError);

        }
    }

    public async Task<ResponseDto<OrderDto>> UpdateOrderStatusAsync(int id, OrderStatus status)
    {
        try
        {
            var order = await _orderRepository.GetAsync(id);
            if (order == null)
            {
                return ResponseDto<OrderDto>.Fail("Sipariş bulunamadı", StatusCodes.Status404NotFound);
            }
            order.OrderStatus = status;
            _orderRepository.Update(order);
            var result = await _uow.SaveAsync();
            if (result > 1)
            {
                return ResponseDto<OrderDto>.Fail("İşlem tamamlanamadı!", StatusCodes.Status500InternalServerError);

            }
            return ResponseDto<OrderDto>.Success(StatusCodes.Status204NoContent);
        }
        catch (System.Exception ex)
        {
            return ResponseDto<OrderDto>.Fail(ex.Message, StatusCodes.Status500InternalServerError);

        }
    }
}
