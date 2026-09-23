using Nirote.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nirote.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly BlobStorageService _blobService;

    public UploadController(BlobStorageService blobService)
    {
        _blobService = blobService;
    }

    [HttpPost("image")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            if (file.Length > 10 * 1024 * 1024)
                return BadRequest("Image must be under 10 MB");

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType))
                return BadRequest("Only JPEG, PNG and WebP images are allowed");

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            using var stream = file.OpenReadStream();
            var url = await _blobService.UploadImageAsync(stream, fileName, file.ContentType);

            return Ok(new { url });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new {
                error = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }

    [HttpPost("video")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UploadVideo(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            if (file.Length > 100 * 1024 * 1024)
                return BadRequest("Video must be under 100 MB");

            var allowedTypes = new[] { "video/mp4", "video/webm", "video/quicktime" };
            if (!allowedTypes.Contains(file.ContentType))
                return BadRequest("Only MP4, WebM and MOV videos are allowed");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext)) ext = ".mp4";
            var fileName = $"video_{Guid.NewGuid()}{ext}";

            using var stream = file.OpenReadStream();
            var url = await _blobService.UploadImageAsync(stream, fileName, file.ContentType);

            return Ok(new { url });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new {
                error = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }

}
