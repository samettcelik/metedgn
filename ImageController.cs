using Microsoft.AspNetCore.Mvc;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DGN_BACK.Data;
using DGN_BACK.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Cors;

namespace DGN_BACK.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors]
    public class ImagesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly Cloudinary _cloudinary;

        public ImagesController(ApplicationDbContext context)
        {
            _context = context;

            // Cloudinary hesabını yapılandır
            Account account = new Account(
                "dddri6fzq",                     // cloud_name
                "375931925232635",              // api_key
                "jig-OapVgJL5s0YagMTCCXgnPkk"    // api_secret
            );
            _cloudinary = new Cloudinary(account);
        }

        // POST: api/images/upload
        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            try
            {
                // Dosya kontrolü
                if (file == null || file.Length == 0)
                {
                    return BadRequest("Dosya seçilmedi");
                }

                // Dosya boyutu kontrolü (50MB)
                const int maxFileSize = 50 * 1024 * 1024; // 50MB
                if (file.Length > maxFileSize)
                {
                    return BadRequest("Dosya boyutu 50MB'dan büyük olamaz");
                }

                // Dosya türü kontrolü
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
                if (!allowedTypes.Contains(file.ContentType.ToLower()))
                {
                    return BadRequest("Sadece resim dosyaları yüklenebilir");
                }

                // Unique dosya adı oluştur
                var publicId = $"wedding_{Guid.NewGuid()}_{DateTime.UtcNow.Ticks}";

                // Cloudinary'e yükle
                using (var stream = file.OpenReadStream())
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(file.FileName, stream),
                        PublicId = publicId,
                        Transformation = new Transformation()
                            .Quality("auto")
                            .FetchFormat("auto")
                    };

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                    if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        return BadRequest("Resim yüklenemedi");
                    }

                    // Optimized URL
                    var optimizedUrl = _cloudinary.Api.UrlImgUp.Secure(true)
                        .Transform(new Transformation().FetchFormat("auto").Quality("auto"))
                        .BuildUrl(publicId);

                    // Cropped URL
                    var croppedUrl = _cloudinary.Api.UrlImgUp.Secure(true)
                        .Transform(new Transformation().Width(800).Height(600).Crop("fill").Gravity("auto"))
                        .BuildUrl(publicId);

                    // Veritabanına kaydet
                    var image = new Image
                    {
                        PublicId = uploadResult.PublicId,
                        OriginalUrl = uploadResult.SecureUrl.ToString(),
                        OptimizedUrl = optimizedUrl,
                        CroppedUrl = croppedUrl,
                        UploadedAt = DateTime.UtcNow
                    };

                    _context.Images.Add(image);
                    await _context.SaveChangesAsync();

                    return Ok(new { 
                        id = image.Id,
                        publicId = image.PublicId,
                        originalUrl = image.OriginalUrl,
                        optimizedUrl = image.OptimizedUrl,
                        croppedUrl = image.CroppedUrl,
                        uploadedAt = image.UploadedAt
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Resim yüklenirken bir hata oluştu: {ex.Message}");
            }
        }

        // GET: api/images
        [HttpGet]
        public async Task<IActionResult> GetImages()
        {
            var images = await _context.Images
                .OrderByDescending(i => i.UploadedAt)
                .ToListAsync();

            return Ok(images);
        }

        // DELETE: api/images/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteImage(int id)
        {
            try
            {
                var image = await _context.Images.FindAsync(id);
                if (image == null)
                {
                    return NotFound();
                }

                // Cloudinary'den sil
                var deleteParams = new DeletionParams(image.PublicId);
                var deleteResult = await _cloudinary.DestroyAsync(deleteParams);

                // Veritabanından sil
                _context.Images.Remove(image);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Resim silindi", deletedId = id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Resim silinirken bir hata oluştu: {ex.Message}");
            }
        }
    }
} 