using System;
using System.Formats.Asn1;
using System.Security.Cryptography.X509Certificates;
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

public class ProductManager : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IImageService _imageService;

    private readonly IGenericRepository<Product> _productRepository;
    private readonly IGenericRepository<Category> _categoryRepository;

    public ProductManager(IUnitOfWork unitOfWork, IMapper mapper, IImageService imageService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _imageService = imageService;
        _productRepository = _unitOfWork.GetRepository<Product>();
        _categoryRepository = _unitOfWork.GetRepository<Category>();

    }

    public async Task<ResponseDto<ProductDto>> AddAsync(ProductCreateDto productCreateDto)
    {
        try
        {
            if (productCreateDto.CategoryIds.Count == 0)
            {
                return ResponseDto<ProductDto>.Fail("En az 1 kategori seçilmelidir", StatusCodes.Status400BadRequest);
            }
            //Kategorilerin veritanında olup olmadığını kontrol et
            foreach (var categoryId in productCreateDto.CategoryIds)
            {
                var categoryExists = await _categoryRepository.ExistsAsync(x => x.Id == categoryId && x.IsActive && !x.IsDeleted);
                if (!categoryExists)
                {
                    return ResponseDto<ProductDto>.Fail($"{categoryId} id'li Kategori Bulunamadı", StatusCodes.Status404NotFound);
                }
            }
            var Product = _mapper.Map<Product>(productCreateDto);
            if (productCreateDto.Image == null)
            {
                return ResponseDto<ProductDto>.Fail("Lütfen bir resimi boş olamaz!", StatusCodes.Status400BadRequest);
            }
            var imageResponse = await _imageService.UploadImageAsync(productCreateDto.Image);
            if (!imageResponse.IsSuccessful && imageResponse.Error != null)
            {
                return ResponseDto<ProductDto>.Fail(imageResponse.Error, imageResponse.StatusCode);
            }
            Product.ImageUrl = imageResponse.Data;
            await _productRepository.AddAsync(Product);
            var result = await _unitOfWork.SaveAsync();
            if (result < 1)
            {
                return ResponseDto<ProductDto>.Fail("Ürün eklenirken bir hata oluştu", StatusCodes.Status500InternalServerError);
            }
            Product.ProductCategories = productCreateDto
                              .CategoryIds
                              .Select(categoryId => new ProductCategory(Product.Id, categoryId))
                              .ToList();
            _productRepository.Update(Product);
            result = await _unitOfWork.SaveAsync();
            if (result < 1)
            {
                return ResponseDto<ProductDto>.Fail("Ürün kategorileri eklenirken bir hata oluştu", StatusCodes.Status500InternalServerError);
            }
            var response = await GetWitchCategoriesAsync(Product.Id);
            if (!response.IsSuccessful && response.Error != null)
            {
                return ResponseDto<ProductDto>.Fail(response.Error, response.StatusCode);
            }
            return ResponseDto<ProductDto>.Success(response.Data, StatusCodes.Status201Created);
        }
        catch (System.Exception ex)
        {
            return ResponseDto<ProductDto>.Fail(ex.Message, StatusCodes.Status500InternalServerError);

        }
    }

    public async Task<ResponseDto<int>> CountAsync()
    {
        try
        {
            var count = await _productRepository.CountAsync();
            return ResponseDto<int>.Success(count, StatusCodes.Status200OK);
        }
        catch (System.Exception ex)
        {

            return ResponseDto<int>.Fail(ex.Message, StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<ResponseDto<int>> CountAsync(bool isActive)
    {
        try
        {
            var count = await _productRepository.CountAsync(x => x.IsActive == isActive);
            return ResponseDto<int>.Success(count, StatusCodes.Status200OK);
        }
        catch (System.Exception ex)
        {

            return ResponseDto<int>.Fail(ex.Message, StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<ResponseDto<IEnumerable<ProductDto>>> GetAllAsync()
    {
        try
        {
            var products = await _productRepository.GetAllAsync();
            if (!products.Any())
            {
                return ResponseDto<IEnumerable<ProductDto>>.Fail("Herhangi bir ürün bulunamadı", StatusCodes.Status404NotFound);

            }
            var productDtos = _mapper.Map<IEnumerable<ProductDto>>(products);
            return ResponseDto<IEnumerable<ProductDto>>.Success(productDtos, StatusCodes.Status200OK);
        }
        catch (System.Exception ex)
        {

            return ResponseDto<IEnumerable<ProductDto>>.Fail(ex.Message, StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<ResponseDto<IEnumerable<ProductDto>>> GetAllAsync(bool isActive)
    {
        try
        {
            var products = await _productRepository.GetAllAsync(
                predicate: x => x.IsActive == isActive); // isimlendirme tekniği ile karışık yazabbiliriz ama null olanı da belirtmek gerekir.


            if (!products.Any())
            {
                return ResponseDto<IEnumerable<ProductDto>>.Fail("Herhangi bir ürün bulunamadı", StatusCodes.Status404NotFound);

            }
            var productDtos = _mapper.Map<IEnumerable<ProductDto>>(products);
            return ResponseDto<IEnumerable<ProductDto>>.Success(productDtos, StatusCodes.Status200OK);
        }
        catch (System.Exception ex)
        {

            return ResponseDto<IEnumerable<ProductDto>>.Fail(ex.Message, StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<ResponseDto<IEnumerable<ProductDto>>> GetAllDeletedAsync()
    {
        try
        {
            var products = await _productRepository.GetAllAsync(
                showIsDeleted: true, predicate: x=>x.IsDeleted
            ); if (!products.Any())
            {
                return ResponseDto<IEnumerable<ProductDto>>.Fail("Herhangi bir silinmiş ürün bulunamadı", StatusCodes.Status404NotFound);

            }
            var productDtos = _mapper.Map<IEnumerable<ProductDto>>(products);
            return ResponseDto<IEnumerable<ProductDto>>.Success(productDtos, StatusCodes.Status200OK);
        }
        catch (System.Exception ex)
        {

            return ResponseDto<IEnumerable<ProductDto>>.Fail(ex.Message, StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<ResponseDto<IEnumerable<ProductDto>>> GetAllWithCategoriesAsync()
    {
        try
        {
            var products = await _productRepository.GetAllAsync(
                predicate: x => x.IsActive == true, // isimlendirme tekniği ile karışık yazabbiliriz ama null olanı da belirtmek gerekir.
                orderBy: x => x.OrderByDescending(x => x.CreateDate),
                includes: query => query.Include(x => x.ProductCategories).ThenInclude(x => x.Category)
                );

            if (!products.Any())
            {
                return ResponseDto<IEnumerable<ProductDto>>.Fail("Herhangi bir ürün bulunamadı", StatusCodes.Status404NotFound);

            }
            var productDtos = _mapper.Map<IEnumerable<ProductDto>>(products);
            return ResponseDto<IEnumerable<ProductDto>>.Success(productDtos, StatusCodes.Status200OK);
        }
        catch (System.Exception ex)
        {

            return ResponseDto<IEnumerable<ProductDto>>.Fail(ex.Message, StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<ResponseDto<ProductDto>> GetAsync(int id)
    {
        try
        {
            var product = await _productRepository.GetAsync(id);
            if (product == null)
            {
                return ResponseDto<ProductDto>.Fail("Ürün bulunamadı.", StatusCodes.Status404NotFound);
            }
            if (!product.IsActive)
            {
                return ResponseDto<ProductDto>.Fail("Ürün pasif durumda.", StatusCodes.Status400BadRequest);
            }
            var productDto = _mapper.Map<ProductDto>(product);
            return ResponseDto<ProductDto>.Success(productDto, StatusCodes.Status200OK);
        }
        catch (Exception ex)
        {
            return ResponseDto<ProductDto>.Fail(ex.Message, StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<ResponseDto<IEnumerable<ProductDto>>> GetByCategoriesAsync(int categoryId)
    {
        try
        {
            var products = await _productRepository.GetAllAsync(
                predicate: x => x.IsActive == true && x.ProductCategories.Any(pc => pc.CategoryId == categoryId), // isimlendirme tekniği ile karışık yazabbiliriz ama null olanı da belirtmek gerekir.
                orderBy: x => x.OrderByDescending(x => x.CreateDate),
                includes: query => query.Include(x => x.ProductCategories).ThenInclude(x => x.Category)
                );

            if (!products.Any())
            {
                return ResponseDto<IEnumerable<ProductDto>>.Fail("Bu Kategoriye  Hiçbir ürün bulunamadı", StatusCodes.Status404NotFound);

            }
            var productDtos = _mapper.Map<IEnumerable<ProductDto>>(products);
            return ResponseDto<IEnumerable<ProductDto>>.Success(productDtos, StatusCodes.Status200OK);
        }
        catch (System.Exception ex)
        {

            return ResponseDto<IEnumerable<ProductDto>>.Fail(ex.Message, StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<ResponseDto<ProductDto>> GetWitchCategoriesAsync(int id)
    {
        try
        {
            var product = await _productRepository.GetAsync(
                predicate: x => x.Id == id,
                includes: query => query.Include(x => x.ProductCategories).ThenInclude(x => x.Category)
            );
            if (product == null)
            {
                return ResponseDto<ProductDto>.Fail("Ürün bulunamadı.", StatusCodes.Status404NotFound);
            }
            if (!product.IsActive)
            {
                return ResponseDto<ProductDto>.Fail("Ürün pasif durumda.", StatusCodes.Status400BadRequest);
            }
            var productDto = _mapper.Map<ProductDto>(product);
            return ResponseDto<ProductDto>.Success(productDto, StatusCodes.Status200OK);
        }
        catch (Exception ex)
        {
            return ResponseDto<ProductDto>.Fail(ex.Message, StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<ResponseDto<NoContent>> HardDeleteAsync(int id)
    {
        try
        {
            var product = await _productRepository.GetAsync(id);
            if (product == null)
            {
                return ResponseDto<NoContent>.Fail("Ürün bulunamadı", StatusCodes.Status404NotFound);
            }
            _productRepository.Delete(product);
             var result=await _unitOfWork.SaveAsync();
             if (result<1)
             {
                return ResponseDto<NoContent>.Fail("Silme işleminde bir sorun oluştu",StatusCodes.Status500InternalServerError);
                
             }
            var deleteResponse = _imageService.DeleteImage(product.ImageUrl!);
            if (!deleteResponse.IsSuccessful)
            {
                return ResponseDto<NoContent>.Fail("Resim silinirken sorun oluştu", StatusCodes.Status501NotImplemented);
            }

            return ResponseDto<NoContent>.Success(StatusCodes.Status204NoContent);
        }
        catch (System.Exception ex)
        {

            return ResponseDto<NoContent>.Fail(ex.Message, StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<ResponseDto<NoContent>> SoftDeleteAsync(int id)
    {
        try
        {
            var product = await _productRepository.GetAsync(id);
            if (product == null)
            {
                return ResponseDto<NoContent>.Fail("Ürün bulunamadı", StatusCodes.Status404NotFound);
            }
            product.IsDeleted = !product.IsDeleted;
            if (product.IsDeleted)
            {
                product.IsActive = false;
            }
            _productRepository.Update(product);
            await _unitOfWork.SaveAsync();
            return ResponseDto<NoContent>.Success(StatusCodes.Status204NoContent);
        }
        catch (System.Exception ex)
        {

            return ResponseDto<NoContent>.Fail(ex.Message, StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<ResponseDto<NoContent>> UpdateAsync(ProductUpdateDto productUpdateDto)
    {
        try
        {
            var product = await _productRepository.GetAsync(
                predicate: x => x.Id == productUpdateDto.Id,
                includes: query => query.Include(x => x.ProductCategories)
                );
            if (product == null)
            {
                return ResponseDto<NoContent>.Fail("Ürün bulunamadı", StatusCodes.Status404NotFound);
            }
            if (!product.IsActive)
            {
                return ResponseDto<NoContent>.Fail("Ürün aktif değil", StatusCodes.Status400BadRequest);
            }
            if (productUpdateDto.CategoryIds.Count == 0)
            {
                return ResponseDto<NoContent>.Fail("Kategori eklemek zorundasınız!", StatusCodes.Status400BadRequest);

            }

            foreach (var categoryId in productUpdateDto.CategoryIds)
            {
                var categoryExists = await _categoryRepository.ExistsAsync(x => x.Id == categoryId && x.IsActive && !x.IsDeleted);
                if (!categoryExists)
                {
                    return ResponseDto<NoContent>.Fail($"{categoryId} id'li Kategori Bulunamadı", StatusCodes.Status404NotFound);
                }
            }
            if (productUpdateDto.Image != null)
            {
                var imageResponse = await _imageService.UploadImageAsync(productUpdateDto.Image);
                if (!imageResponse.IsSuccessful && imageResponse.Error != null)
                {
                    return ResponseDto<NoContent>.Fail(imageResponse.Error, imageResponse.StatusCode);
                }
                //şimdi ürünün eski resmini sileceğiz
                var deleteResponse = _imageService.DeleteImage(product.ImageUrl!);
                if (!deleteResponse.IsSuccessful)
                {
                    return ResponseDto<NoContent>.Fail("Resim silinirken sorun oluştu", StatusCodes.Status501NotImplemented);
                }
                product.ImageUrl = imageResponse.Data;

            }
            _mapper.Map(productUpdateDto, product); // productUpdateDto daki verileri product a map eder.

            product.ProductCategories.Clear(); // bir sürü karşılaştırma yapıcağımıza göre direkt kategorileri silelim ve tekrar ekleyelim.
            product.ProductCategories = productUpdateDto
                .CategoryIds
                .Select(categoryId => new ProductCategory(product.Id, categoryId))
                .ToList();
            _productRepository.Update(product);
            var result = await _unitOfWork.SaveAsync();
            if (result < 1)
            {
                return ResponseDto<NoContent>.Fail("Ürün güncellenirken bir hata oluştu", StatusCodes.Status500InternalServerError);
            }
            return ResponseDto<NoContent>.Success(StatusCodes.Status204NoContent);
        }
        catch (System.Exception ex)
        {

            return ResponseDto<NoContent>.Fail(ex.Message, StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<ResponseDto<bool>> UpdateIsActiveAsync(int id)
    {
        try
        {
            var product = await _productRepository.GetAsync(id);
            if (product == null)
            {
                return ResponseDto<bool>.Fail("Ürün bulunamadı", StatusCodes.Status404NotFound);
            }
            product.IsActive = !product.IsActive;
            _productRepository.Update(product);
            var result = await _unitOfWork.SaveAsync();
            if (result < 1)
            {
                return ResponseDto<bool>.Fail("Ürün güncellenirken bir hata oluştu", StatusCodes.Status500InternalServerError);
            }
            return ResponseDto<bool>.Success(product.IsActive, StatusCodes.Status204NoContent);
        }
        catch (System.Exception ex)
        {

            return ResponseDto<bool>.Fail(ex.Message, StatusCodes.Status500InternalServerError);
        }
    }
}
