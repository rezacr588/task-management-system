using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApi.Infrastructure.Services;

namespace TodoApi.WebApi.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<FilesController> _logger;

    public FilesController(IFileStorageService fileStorageService, ILogger<FilesController> logger)
    {
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a file
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(FileUploadResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FileUploadResult>> UploadFile(
        IFormFile file,
        [FromForm] int? todoItemId = null,
        [FromForm] string? description = null)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            var userId = GetCurrentUserId();
            
            using var stream = file.OpenReadStream();
            var request = new FileUploadRequest
            {
                FileStream = stream,
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                UserId = userId,
                TodoItemId = todoItemId,
                Description = description
            };

            var result = await _fileStorageService.UploadFileAsync(request);
            
            if (!result.IsSuccessful)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File upload failed");
            return StatusCode(500, "File upload failed");
        }
    }

    /// <summary>
    /// Upload multiple files
    /// </summary>
    [HttpPost("upload/multiple")]
    [ProducesResponseType(typeof(FileUploadResult[]), StatusCodes.Status200OK)]
    public async Task<ActionResult<FileUploadResult[]>> UploadMultipleFiles(
        List<IFormFile> files,
        [FromForm] int? todoItemId = null)
    {
        try
        {
            if (files == null || !files.Any())
            {
                return BadRequest("No files uploaded");
            }

            var userId = GetCurrentUserId();
            var requests = new List<FileUploadRequest>();

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var stream = file.OpenReadStream();
                    requests.Add(new FileUploadRequest
                    {
                        FileStream = stream,
                        FileName = file.FileName,
                        ContentType = file.ContentType,
                        FileSize = file.Length,
                        UserId = userId,
                        TodoItemId = todoItemId
                    });
                }
            }

            var results = await _fileStorageService.UploadMultipleFilesAsync(requests.ToArray());
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Multiple file upload failed");
            return StatusCode(500, "File upload failed");
        }
    }

    /// <summary>
    /// Download a file
    /// </summary>
    [HttpGet("{fileId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadFile(string fileId)
    {
        try
        {
            var downloadResult = await _fileStorageService.DownloadFileAsync(fileId);
            
            if (downloadResult == null)
            {
                return NotFound();
            }

            return File(downloadResult.FileStream, downloadResult.ContentType, downloadResult.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File download failed for {FileId}", fileId);
            return StatusCode(500, "File download failed");
        }
    }

    /// <summary>
    /// Get file metadata
    /// </summary>
    [HttpGet("{fileId}/metadata")]
    [ProducesResponseType(typeof(FileMetadata), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FileMetadata>> GetFileMetadata(string fileId)
    {
        try
        {
            var metadata = await _fileStorageService.GetFileMetadataAsync(fileId);
            
            if (metadata == null)
            {
                return NotFound();
            }

            return Ok(metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get metadata for {FileId}", fileId);
            return StatusCode(500, "Failed to get file metadata");
        }
    }

    /// <summary>
    /// Get file thumbnail
    /// </summary>
    [HttpGet("{fileId}/thumbnail")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetThumbnail(string fileId, [FromQuery] ThumbnailSize size = ThumbnailSize.Medium)
    {
        try
        {
            var thumbnailUrl = await _fileStorageService.GetThumbnailUrlAsync(fileId, size);
            
            if (thumbnailUrl == null)
            {
                return NotFound();
            }

            // For now, redirect to the thumbnail URL
            // In a real implementation, you might serve the thumbnail directly
            return Redirect(thumbnailUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get thumbnail for {FileId}", fileId);
            return NotFound();
        }
    }

    /// <summary>
    /// Delete a file
    /// </summary>
    [HttpDelete("{fileId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFile(string fileId)
    {
        try
        {
            var deleted = await _fileStorageService.DeleteFileAsync(fileId);
            
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File deletion failed for {FileId}", fileId);
            return StatusCode(500, "File deletion failed");
        }
    }

    /// <summary>
    /// Get file analysis (security scan, content analysis)
    /// </summary>
    [HttpGet("{fileId}/analysis")]
    [ProducesResponseType(typeof(FileAnalysisResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FileAnalysisResult>> AnalyzeFile(string fileId)
    {
        try
        {
            var analysis = await _fileStorageService.AnalyzeFileAsync(fileId);
            
            if (analysis == null)
            {
                return NotFound();
            }

            return Ok(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File analysis failed for {FileId}", fileId);
            return StatusCode(500, "File analysis failed");
        }
    }

    /// <summary>
    /// Get storage analytics for current user
    /// </summary>
    [HttpGet("analytics")]
    [ProducesResponseType(typeof(StorageAnalytics), StatusCodes.Status200OK)]
    public async Task<ActionResult<StorageAnalytics>> GetStorageAnalytics()
    {
        try
        {
            var userId = GetCurrentUserId();
            var analytics = await _fileStorageService.GetStorageAnalyticsAsync(userId);
            
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get storage analytics");
            return StatusCode(500, "Failed to get storage analytics");
        }
    }

    /// <summary>
    /// Generate signed upload URL for direct upload
    /// </summary>
    [HttpPost("upload-url")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GenerateUploadUrl([FromBody] GenerateUploadUrlRequest request)
    {
        try
        {
            var uploadUrl = await _fileStorageService.GenerateUploadUrlAsync(
                request.FileName, 
                request.ContentType, 
                TimeSpan.FromHours(1));
            
            if (uploadUrl == null)
            {
                return BadRequest("Failed to generate upload URL");
            }

            return Ok(new { uploadUrl, expiresIn = 3600 });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate upload URL");
            return StatusCode(500, "Failed to generate upload URL");
        }
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst("userId")?.Value ?? 
               User.FindFirst("sub")?.Value ?? 
               User.Identity?.Name ?? 
               "anonymous";
    }
}

public class GenerateUploadUrlRequest
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}