namespace TodoApi.Infrastructure.Services;

public interface IFileStorageService
{
    // File upload operations
    Task<FileUploadResult> UploadFileAsync(FileUploadRequest request);
    Task<FileUploadResult[]> UploadMultipleFilesAsync(FileUploadRequest[] requests);
    
    // File retrieval operations
    Task<FileDownloadResult?> DownloadFileAsync(string fileId);
    Task<Stream?> GetFileStreamAsync(string fileId);
    Task<string?> GetFileUrlAsync(string fileId, TimeSpan? expiration = null);
    Task<string?> GetThumbnailUrlAsync(string fileId, ThumbnailSize size = ThumbnailSize.Medium);
    
    // File management operations
    Task<bool> DeleteFileAsync(string fileId);
    Task<bool> FileExistsAsync(string fileId);
    Task<FileMetadata?> GetFileMetadataAsync(string fileId);
    Task<FileMetadata[]> GetFileMetadataAsync(string[] fileIds);
    
    // File organization
    Task<bool> MoveFileAsync(string fileId, string newPath);
    Task<bool> CopyFileAsync(string fileId, string newPath);
    Task<FileMetadata[]> ListFilesAsync(string path, string? pattern = null);
    
    // Advanced operations
    Task<string?> GenerateUploadUrlAsync(string fileName, string contentType, TimeSpan? expiration = null);
    Task<bool> ProcessImageAsync(string fileId, ImageProcessingOptions options);
    Task<FileAnalysisResult?> AnalyzeFileAsync(string fileId);
    
    // Storage analytics
    Task<StorageAnalytics> GetStorageAnalyticsAsync(string? userId = null);
    Task CleanupExpiredFilesAsync();
}

public class FileUploadRequest
{
    public Stream FileStream { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? UserId { get; set; }
    public int? TodoItemId { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
    public bool GenerateThumbnail { get; set; } = true;
    public bool ScanForViruses { get; set; } = true;
    public string? FolderPath { get; set; }
}

public class FileUploadResult
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class FileDownloadResult
{
    public Stream FileStream { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime LastModified { get; set; }
    public string? ETag { get; set; }
}

public class FileMetadata
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? UserId { get; set; }
    public int? TodoItemId { get; set; }
    public string? Description { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? LastModified { get; set; }
    public string? FolderPath { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
    public bool HasThumbnail { get; set; }
    public bool IsImage { get; set; }
    public bool IsPublic { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Hash { get; set; }
}

public class ImageProcessingOptions
{
    public int? Width { get; set; }
    public int? Height { get; set; }
    public ImageFormat Format { get; set; } = ImageFormat.Original;
    public int Quality { get; set; } = 85;
    public bool MaintainAspectRatio { get; set; } = true;
    public ResizeMode ResizeMode { get; set; } = ResizeMode.Crop;
    public bool GenerateThumbnail { get; set; } = true;
    public ThumbnailSize[] ThumbnailSizes { get; set; } = { ThumbnailSize.Small, ThumbnailSize.Medium, ThumbnailSize.Large };
}

public class FileAnalysisResult
{
    public string FileId { get; set; } = string.Empty;
    public bool IsSafe { get; set; } = true;
    public string[] DetectedThreats { get; set; } = Array.Empty<string>();
    public string? FileType { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    public string? ExtractedText { get; set; }
    public ImageAnalysis? ImageAnalysis { get; set; }
    public DateTime AnalyzedAt { get; set; }
}

public class ImageAnalysis
{
    public int Width { get; set; }
    public int Height { get; set; }
    public string[] DominantColors { get; set; } = Array.Empty<string>();
    public string[] DetectedObjects { get; set; } = Array.Empty<string>();
    public string? Description { get; set; }
    public bool HasFaces { get; set; }
    public bool IsScreenshot { get; set; }
    public double Quality { get; set; }
}

public class StorageAnalytics
{
    public long TotalFiles { get; set; }
    public long TotalSizeBytes { get; set; }
    public Dictionary<string, long> FilesByType { get; set; } = new();
    public Dictionary<string, long> FilesByUser { get; set; } = new();
    public long FilesUploadedToday { get; set; }
    public long FilesUploadedThisWeek { get; set; }
    public long FilesUploadedThisMonth { get; set; }
    public string? LargestFile { get; set; }
    public DateTime? OldestFile { get; set; }
    public double AverageFileSize { get; set; }
}

public enum ThumbnailSize
{
    Small = 150,
    Medium = 300,
    Large = 600
}

public enum ImageFormat
{
    Original,
    Jpeg,
    Png,
    WebP,
    Avif
}

public enum ResizeMode
{
    Crop,
    Fit,
    Stretch,
    Pad
}