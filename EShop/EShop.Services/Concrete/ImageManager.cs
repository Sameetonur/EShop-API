using System;
using EShop.Services.Abstract;
using EShop.Shared.Dtos.ResponseDtos;
using Microsoft.AspNetCore.Http;

namespace EShop.Services.Concrete;

public class ImageManager : IImageService
{
    private readonly String _imageFolderPath;
    public ImageManager()
    {
        //D:\GitHubb\BackendMasteryBootcamp\03-API\Week07\19-01-2024\v1\EShop\EShop.API\wwwroot\images
        _imageFolderPath = Path.Combine(Directory.GetCurrentDirectory(),"wwwroot","images");
        if (!Directory.Exists(_imageFolderPath))
        {
            Directory.CreateDirectory(_imageFolderPath);
            
        }
    }
    public  ResponseDto<NoContent> DeleteImage(string imageUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return ResponseDto<NoContent>.Fail("Resim yolu boş olamaz",StatusCodes.Status400BadRequest);
                
            }

            var FileName = Path.GetFileName(imageUrl);
            var fileFullPath= Path.Combine(_imageFolderPath,FileName);
            if (!File.Exists(fileFullPath))
            {
                return ResponseDto<NoContent>.Fail("Resim dosyası bulunamadı", StatusCodes.Status400BadRequest);
            }
            File.Delete(fileFullPath);
            return ResponseDto<NoContent>.Success(StatusCodes.Status200OK);
        }
        catch (System.Exception ex)
        {
            return ResponseDto<NoContent>.Fail( ex.Message,StatusCodes.Status500InternalServerError);
            
        }
    }

    public async Task<ResponseDto<string>> UploadImageAsync(IFormFile image)
    {
        try
        {
            if (image==null)
            {
                return ResponseDto<string>.Fail("Resim dosyası boş olamaz!",StatusCodes.Status400BadRequest);
                
            }
            if (image.Length==0)
            {

                return ResponseDto<string>.Fail("Resim dosyası 0byte'den büyük olmalıdır!", StatusCodes.Status400BadRequest);
            }
             var allowedExtensions = new[] {".jpg",".jpeg",".png",".bmp",".gif"};
             var imageExtension= Path.GetExtension(image.FileName);//.png
             if(!allowedExtensions.Contains(imageExtension))//contains dizinin içinde var mı yok muyu kontrol ediyor
             {
                return ResponseDto<string>.Fail("Uygunsuz dosya uzantısı!", StatusCodes.Status400BadRequest);
            }
            if (image.Length >5*1024*1024)
            {
                return ResponseDto<string>.Fail("Resim dosyası 5MB'tan büyük olamaz!", StatusCodes.Status400BadRequest);
            }
            var FileName =$"{Guid.NewGuid()}{imageExtension}"; // 4a5d4asd-as614d54-564a1d-6a5s1d5.png
            var fileFullPath = Path.Combine(_imageFolderPath,FileName);
            //D:\GitHubb\BackendMasteryBootcamp\03-API\Week07\19-01-2024\v1\EShop\EShop.API\wwwroot\images\4a5d4asd-as614d54-564a1d-6a5s1d5.png

            using (var stream = new FileStream(fileFullPath,FileMode.Create))
            {
                await image.CopyToAsync(stream);
                
            }
             return ResponseDto<string>.Success($"/images/{FileName}",StatusCodes.Status201Created);
        }
        catch (System.Exception ex)
        {
            
            return ResponseDto<string>.Fail(ex.Message,StatusCodes.Status500InternalServerError);
        }
    }
}
