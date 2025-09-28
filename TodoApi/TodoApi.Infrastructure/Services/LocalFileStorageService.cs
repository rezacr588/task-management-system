using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text.Json;
using TodoApi.Infrastructure.Logging;

namespace TodoApi.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IStructuredLogger<LocalFileStorageService> _logger;
    private readonly IMetricsCollector _metrics;
    private readonly string _storagePath;
    private readonly string _metadataPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public LocalFileStorageService(
        IStructuredLogger<LocalFileStorageService> logger,
        IMetricsCollector metrics,
        IConfiguration configuration)
    {
        _logger = logger;
        _metrics = metrics;
        _storagePath = configuration["FileStorage:LocalPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        _metadataPath = Path.Combine(_storagePath, "metadata");
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        // Ensure directories exist
        Directory.CreateDirectory(_storagePath);
        Directory.CreateDirectory(_metadataPath);
    }

    public async Task<FileUploadResult> UploadFileAsync(FileUploadRequest request)
    {
        using var scope = _logger.BeginScope("UploadFile", new { FileName = request.FileName, FileSize = request.FileSize });
        var startTime = DateTime.UtcNow;

        try
        {
            // Validate file
            var validationResult = ValidateFile(request);
            if (!validationResult.IsValid)
            {
                return new FileUploadResult
                {
                    IsSuccessful = false,
                    ErrorMessage = validationResult.ErrorMessage
                };
            }

            // Generate unique file ID and paths
            var fileId = Guid.NewGuid().ToString();
            var fileExtension = Path.GetExtension(request.FileName);
            var sanitizedFileName = SanitizeFileName(Path.GetFileNameWithoutExtension(request.FileName));
            var storedFileName = $"{fileId}_{sanitizedFileName}{fileExtension}";
            
            var userFolder = !string.IsNullOrEmpty(request.UserId) ? request.UserId : "anonymous";
            var dateFolder = DateTime.UtcNow.ToString("yyyy/MM/dd");
            var relativePath = Path.Combine(userFolder, dateFolder);
            var fullFolderPath = Path.Combine(_storagePath, relativePath);
            
            Directory.CreateDirectory(fullFolderPath);
            
            var filePath = Path.Combine(fullFolderPath, storedFileName);

            // Calculate hash while uploading
            string fileHash;
            using (var sha256 = SHA256.Create())
            {
                request.FileStream.Position = 0;
                var hashBytes = await sha256.ComputeHashAsync(request.FileStream);
                fileHash = Convert.ToHexString(hashBytes).ToLower();
            }

            // Reset stream position and copy file
            request.FileStream.Position = 0;
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await request.FileStream.CopyToAsync(fileStream);

            // Create metadata
            var metadata = new FileMetadata
            {
                FileId = fileId,
                FileName = storedFileName,
                OriginalFileName = request.FileName,
                ContentType = request.ContentType,
                FileSize = request.FileSize,
                UserId = request.UserId,
                TodoItemId = request.TodoItemId,
                Description = request.Description,
                UploadedAt = DateTime.UtcNow,
                FolderPath = relativePath,
                Metadata = request.Metadata,
                IsImage = IsImageFile(request.ContentType),
                Hash = fileHash
            };

            // Save metadata
            await SaveMetadataAsync(metadata);

            // Generate thumbnail for images
            string? thumbnailUrl = null;
            if (metadata.IsImage && request.GenerateThumbnail)
            {
                try
                {
                    thumbnailUrl = await GenerateThumbnailAsync(filePath, fileId);
                    metadata.HasThumbnail = !string.IsNullOrEmpty(thumbnailUrl);
                    await SaveMetadataAsync(metadata); // Update metadata with thumbnail info
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to generate thumbnail for {FileId}: {Error}", fileId, ex.Message);
                }
            }

            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordHistogram("file_upload_duration", duration.TotalMilliseconds, 
                ("file_type", request.ContentType), ("file_size_mb", (request.FileSize / 1024.0 / 1024.0).ToString("F1")));

            var result = new FileUploadResult
            {
                FileId = fileId,
                FileName = request.FileName,
                Url = GenerateFileUrl(fileId),
                ThumbnailUrl = thumbnailUrl,
                FileSize = request.FileSize,
                ContentType = request.ContentType,
                UploadedAt = metadata.UploadedAt,
                IsSuccessful = true,
                Metadata = metadata.Metadata
            };

            _logger.LogInformation("File uploaded successfully: {FileId} ({FileSize} bytes) in {Duration}ms", 
                fileId, request.FileSize, duration.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError("File upload failed: {Error}", ex, ex.Message);
            _metrics.IncrementErrorCounter("file_upload_error");
            return new FileUploadResult
            {
                IsSuccessful = false,
                ErrorMessage = $"Upload failed: {ex.Message}"
            };
        }
    }

    public async Task<FileUploadResult[]> UploadMultipleFilesAsync(FileUploadRequest[] requests)
    {
        var results = new List<FileUploadResult>();
        
        foreach (var request in requests)
        {
            var result = await UploadFileAsync(request);
            results.Add(result);
        }
        
        return results.ToArray();
    }

    public async Task<FileDownloadResult?> DownloadFileAsync(string fileId)
    {
        using var scope = _logger.BeginScope("DownloadFile", new { FileId = fileId });

        try
        {
            var metadata = await GetFileMetadataAsync(fileId);
            if (metadata == null)
            {
                _logger.LogWarning("File not found: {FileId}", fileId);
                return null;
            }

            var filePath = Path.Combine(_storagePath, metadata.FolderPath ?? "", metadata.FileName);
            
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Physical file not found: {FilePath}", filePath);
                return null;
            }

            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var fileInfo = new FileInfo(filePath);

            _logger.LogTrace("File download initiated: {FileId}", fileId);

            return new FileDownloadResult
            {
                FileStream = fileStream,
                FileName = metadata.OriginalFileName,
                ContentType = metadata.ContentType,
                FileSize = metadata.FileSize,
                LastModified = fileInfo.LastWriteTime,
                ETag = metadata.Hash
            };
        }
        catch (Exception ex)
        {
            _logger.LogError("File download failed for {FileId}: {Error}", fileId, ex, ex.Message);
            return null;
        }
    }

    public async Task<Stream?> GetFileStreamAsync(string fileId)
    {
        var downloadResult = await DownloadFileAsync(fileId);
        return downloadResult?.FileStream;
    }

    public async Task<string?> GetFileUrlAsync(string fileId, TimeSpan? expiration = null)
    {
        var metadata = await GetFileMetadataAsync(fileId);
        if (metadata == null)
            return null;

        // For local storage, return a simple URL (in production, this would be signed URLs)
        return GenerateFileUrl(fileId);
    }

    public async Task<string?> GetThumbnailUrlAsync(string fileId, ThumbnailSize size = ThumbnailSize.Medium)
    {
        var metadata = await GetFileMetadataAsync(fileId);
        if (metadata == null || !metadata.HasThumbnail)
            return null;

        return GenerateThumbnailUrl(fileId, size);
    }

    public async Task<bool> DeleteFileAsync(string fileId)
    {
        using var scope = _logger.BeginScope("DeleteFile", new { FileId = fileId });

        try
        {
            var metadata = await GetFileMetadataAsync(fileId);
            if (metadata == null)
                return false;

            // Delete physical file
            var filePath = Path.Combine(_storagePath, metadata.FolderPath ?? "", metadata.FileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            // Delete thumbnails
            if (metadata.HasThumbnail)
            {
                foreach (ThumbnailSize size in Enum.GetValues<ThumbnailSize>())
                {
                    var thumbnailPath = GetThumbnailPath(fileId, size);
                    if (File.Exists(thumbnailPath))
                    {
                        File.Delete(thumbnailPath);
                    }
                }
            }

            // Delete metadata
            var metadataFilePath = Path.Combine(_metadataPath, $"{fileId}.json");
            if (File.Exists(metadataFilePath))
            {
                File.Delete(metadataFilePath);
            }

            _logger.LogInformation("File deleted successfully: {FileId}", fileId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to delete file {FileId}: {Error}", fileId, ex, ex.Message);
            return false;
        }
    }

    public async Task<bool> FileExistsAsync(string fileId)
    {
        var metadata = await GetFileMetadataAsync(fileId);
        if (metadata == null)
            return false;

        var filePath = Path.Combine(_storagePath, metadata.FolderPath ?? "", metadata.FileName);
        return File.Exists(filePath);
    }

    public async Task<FileMetadata?> GetFileMetadataAsync(string fileId)
    {
        try
        {
            var metadataFilePath = Path.Combine(_metadataPath, $"{fileId}.json");
            
            if (!File.Exists(metadataFilePath))
                return null;

            var json = await File.ReadAllTextAsync(metadataFilePath);
            return JsonSerializer.Deserialize<FileMetadata>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to read metadata for {FileId}: {Error}", fileId, ex, ex.Message);
            return null;
        }
    }

    public async Task<FileMetadata[]> GetFileMetadataAsync(string[] fileIds)
    {
        var results = new List<FileMetadata>();
        
        foreach (var fileId in fileIds)
        {
            var metadata = await GetFileMetadataAsync(fileId);
            if (metadata != null)
            {
                results.Add(metadata);
            }
        }
        
        return results.ToArray();
    }

    public async Task<bool> MoveFileAsync(string fileId, string newPath)
    {
        // Implementation for moving files
        await Task.CompletedTask;
        return false; // Placeholder
    }

    public async Task<bool> CopyFileAsync(string fileId, string newPath)
    {
        // Implementation for copying files
        await Task.CompletedTask;
        return false; // Placeholder
    }

    public async Task<FileMetadata[]> ListFilesAsync(string path, string? pattern = null)
    {
        // Implementation for listing files
        await Task.CompletedTask;
        return Array.Empty<FileMetadata>(); // Placeholder
    }

    public async Task<string?> GenerateUploadUrlAsync(string fileName, string contentType, TimeSpan? expiration = null)
    {
        // For local storage, this would return an upload endpoint
        await Task.CompletedTask;
        return null; // Placeholder
    }

    public async Task<bool> ProcessImageAsync(string fileId, ImageProcessingOptions options)
    {
        // Implementation for image processing
        await Task.CompletedTask;
        return false; // Placeholder
    }

    public async Task<FileAnalysisResult?> AnalyzeFileAsync(string fileId)
    {
        var metadata = await GetFileMetadataAsync(fileId);
        if (metadata == null)
            return null;

        var result = new FileAnalysisResult
        {
            FileId = fileId,
            IsSafe = true, // Basic implementation - always safe
            FileType = metadata.ContentType,
            AnalyzedAt = DateTime.UtcNow
        };

        // Add image analysis if it's an image
        if (metadata.IsImage)
        {
            result.ImageAnalysis = new ImageAnalysis
            {
                Width = 0, // Would extract from image
                Height = 0,
                DominantColors = Array.Empty<string>(),
                Quality = 85.0
            };
        }

        return result;
    }

    public async Task<StorageAnalytics> GetStorageAnalyticsAsync(string? userId = null)
    {
        var metadataFiles = Directory.GetFiles(_metadataPath, "*.json");
        var analytics = new StorageAnalytics();

        var tasks = metadataFiles.Select(async file =>
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                return JsonSerializer.Deserialize<FileMetadata>(json, _jsonOptions);
            }
            catch
            {
                return null;
            }
        });

        var allMetadata = (await Task.WhenAll(tasks)).Where(m => m != null).ToArray();

        if (userId != null)
        {
            allMetadata = allMetadata.Where(m => m!.UserId == userId).ToArray();
        }

        analytics.TotalFiles = allMetadata.Length;
        analytics.TotalSizeBytes = allMetadata.Sum(m => m!.FileSize);
        analytics.AverageFileSize = analytics.TotalFiles > 0 ? (double)analytics.TotalSizeBytes / analytics.TotalFiles : 0;

        var today = DateTime.UtcNow.Date;
        analytics.FilesUploadedToday = allMetadata.Count(m => m!.UploadedAt.Date == today);
        analytics.FilesUploadedThisWeek = allMetadata.Count(m => m!.UploadedAt >= today.AddDays(-7));
        analytics.FilesUploadedThisMonth = allMetadata.Count(m => m!.UploadedAt >= today.AddDays(-30));

        if (allMetadata.Any())
        {
            var oldestFile = allMetadata.OrderBy(m => m!.UploadedAt).First();
            analytics.OldestFile = oldestFile!.OriginalFileName;
            
            var largestFile = allMetadata.OrderByDescending(m => m!.FileSize).First();
            analytics.LargestFile = largestFile!.OriginalFileName;
        }

        return analytics;
    }

    public async Task CleanupExpiredFilesAsync()
    {
        using var scope = _logger.BeginScope("CleanupExpiredFiles");
        var cleanedCount = 0;

        try
        {
            var metadataFiles = Directory.GetFiles(_metadataPath, "*.json");
            var now = DateTime.UtcNow;

            foreach (var metadataFile in metadataFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(metadataFile);
                    var metadata = JsonSerializer.Deserialize<FileMetadata>(json, _jsonOptions);
                    
                    if (metadata?.ExpiresAt.HasValue == true && metadata.ExpiresAt.Value < now)
                    {
                        await DeleteFileAsync(metadata.FileId);
                        cleanedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to process metadata file {File}: {Error}", metadataFile, ex.Message);
                }
            }

            _logger.LogInformation("Cleanup completed: {CleanedCount} expired files removed", cleanedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError("Cleanup failed: {Error}", ex, ex.Message);
        }
    }

    private async Task SaveMetadataAsync(FileMetadata metadata)
    {
        var metadataFilePath = Path.Combine(_metadataPath, $"{metadata.FileId}.json");
        var json = JsonSerializer.Serialize(metadata, _jsonOptions);
        await File.WriteAllTextAsync(metadataFilePath, json);
    }

    private static (bool IsValid, string? ErrorMessage) ValidateFile(FileUploadRequest request)
    {
        const long maxFileSize = 50 * 1024 * 1024; // 50 MB
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".txt", ".zip" };

        if (request.FileSize > maxFileSize)
        {
            return (false, "File size exceeds maximum allowed size of 50 MB");
        }

        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return (false, $"File type '{extension}' is not allowed");
        }

        return (true, null);
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    }

    private static bool IsImageFile(string contentType)
    {
        return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    private string GenerateFileUrl(string fileId)
    {
        return $"/api/files/{fileId}";
    }

    private string GenerateThumbnailUrl(string fileId, ThumbnailSize size)
    {
        return $"/api/files/{fileId}/thumbnail?size={size}";
    }

    private async Task<string?> GenerateThumbnailAsync(string originalFilePath, string fileId)
    {
        // Placeholder for thumbnail generation
        // In a real implementation, you would use ImageSharp or similar library
        try
        {
            var thumbnailDir = Path.Combine(_storagePath, "thumbnails", fileId);
            Directory.CreateDirectory(thumbnailDir);
            
            // This is a placeholder - actual thumbnail generation would go here
            await Task.Delay(10); // Simulate processing time
            
            return GenerateThumbnailUrl(fileId, ThumbnailSize.Medium);
        }
        catch
        {
            return null;
        }
    }

    private string GetThumbnailPath(string fileId, ThumbnailSize size)
    {
        return Path.Combine(_storagePath, "thumbnails", fileId, $"{size}.jpg");
    }
}