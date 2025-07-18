using Microsoft.AspNetCore.Mvc;
using DGN_BACK.Data;
using DGN_BACK.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Cors;

namespace DGN_BACK.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors]
    public class MessagesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MessagesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateMessage([FromBody] MessageDto dto)
        {
            try
            {
                Console.WriteLine("🚀 CreateMessage called");
                Console.WriteLine($"📝 Content: {dto.Content}");
                Console.WriteLine($"👤 GondericiAdi: {dto.GondericiAdi}");
                Console.WriteLine($"🖼️ ImageIds: {string.Join(", ", dto.ImageIds ?? new List<int>())}");
                
                // Validation - Gönderici adı zorunlu
                if (string.IsNullOrWhiteSpace(dto.GondericiAdi))
                {
                    return BadRequest("Gönderici adı zorunludur");
                }
                
                // Validation - Mesaj veya resim zorunlu
                if (string.IsNullOrWhiteSpace(dto.Content) && (dto.ImageIds == null || !dto.ImageIds.Any()))
                {
                    return BadRequest("Mesaj içeriği veya en az bir resim gereklidir");
                }

                var message = new Message
                {
                    Content = dto.Content?.Trim() ?? string.Empty,
                    GondericiAdi = dto.GondericiAdi?.Trim() ?? string.Empty,
                    CreatedAt = DateTime.UtcNow
                };

                // Resim kontrolü ve ekleme
                if (dto.ImageIds != null && dto.ImageIds.Any())
                {
                    // OPENJSON hatasını önlemek için ImageIds'i local variable'a ata
                    var imageIdList = dto.ImageIds.Distinct().ToList();
                    
                    // Maksimum 4 resim kontrolü
                    if (imageIdList.Count > 4)
                    {
                        return BadRequest("En fazla 4 resim ekleyebilirsiniz");
                    }
                    
                    // OPENJSON hatası önlenir - En basit çözüm
                    var existingImages = new List<int>();
                    
                    // Her image ID'yi tek tek kontrol et
                    foreach (var imageId in imageIdList)
                    {
                        var exists = await _context.Images
                            .Where(i => i.Id == imageId)
                            .AnyAsync();
                        
                        if (exists)
                        {
                            existingImages.Add(imageId);
                        }
                    }

                    // Var olan resimleri message'a ekle
                    foreach (var imageId in existingImages)
                    {
                        message.MessageImages.Add(new MessageImage
                        {
                            ImageId = imageId
                        });
                    }

                    // Var olmayan resim ID'leri için uyarı
                    var nonExistentIds = imageIdList.Except(existingImages).ToList();
                    if (nonExistentIds.Any())
                    {
                        return BadRequest($"Şu resim ID'leri bulunamadı: {string.Join(", ", nonExistentIds)}");
                    }
                }

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // Tam veri ile response döndür
                var createdMessage = await _context.Messages
                    .Include(m => m.MessageImages)
                    .ThenInclude(mi => mi.Image)
                    .FirstOrDefaultAsync(m => m.Id == message.Id);

                if (createdMessage == null)
                {
                    return StatusCode(500, "Oluşturulan mesaj bulunamadı");
                }

                return Ok(new
                {
                    id = createdMessage.Id,
                    content = createdMessage.Content,
                    gondericiAdi = createdMessage.GondericiAdi,
                    createdAt = createdMessage.CreatedAt,
                    messageImages = createdMessage.MessageImages.Select(mi => new
                    {
                        id = mi.Id,
                        imageId = mi.ImageId,
                        image = mi.Image != null ? new
                        {
                            id = mi.Image.Id,
                            publicId = mi.Image.PublicId,
                            originalUrl = mi.Image.OriginalUrl,
                            optimizedUrl = mi.Image.OptimizedUrl,
                            croppedUrl = mi.Image.CroppedUrl,
                            uploadedAt = mi.Image.UploadedAt
                        } : null
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Mesaj oluşturulurken bir hata oluştu: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages()
        {
            try
            {
                var messages = await _context.Messages
                    .Include(m => m.MessageImages)
                    .ThenInclude(mi => mi.Image)
                    .OrderByDescending(m => m.CreatedAt)
                    .ToListAsync();

                var result = messages.Select(m => new
                {
                    id = m.Id,
                    content = m.Content,
                    gondericiAdi = m.GondericiAdi,
                    createdAt = m.CreatedAt,
                    messageImages = m.MessageImages.Select(mi => new
                    {
                        id = mi.Id,
                        imageId = mi.ImageId,
                        image = mi.Image != null ? new
                        {
                            id = mi.Image.Id,
                            publicId = mi.Image.PublicId,
                            originalUrl = mi.Image.OriginalUrl,
                            optimizedUrl = mi.Image.OptimizedUrl,
                            croppedUrl = mi.Image.CroppedUrl,
                            uploadedAt = mi.Image.UploadedAt
                        } : null
                    }).ToList()
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Mesajlar alınırken bir hata oluştu: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            try
            {
                var message = await _context.Messages
                    .Include(m => m.MessageImages)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (message == null)
                {
                    return NotFound();
                }

                _context.Messages.Remove(message);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Mesaj silindi", deletedId = id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Mesaj silinirken bir hata oluştu: {ex.Message}");
            }
        }
    }

    public class MessageDto
    {
        public string Content { get; set; } = string.Empty;
        public string GondericiAdi { get; set; } = string.Empty;
        public List<int> ImageIds { get; set; } = new();
    }
} 