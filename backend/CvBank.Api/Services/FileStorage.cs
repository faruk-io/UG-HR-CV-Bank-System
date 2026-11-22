using CvBank.Api.Models;

namespace CvBank.Api.Services;

public sealed record StoredUpload(string RelativePath, string FileName, string ContentType, long SizeBytes);

public interface IFileStorage
{
    long MaxUploadBytes { get; }

    void Validate(string originalFileName, string? contentType, long sizeBytes, DocumentCategory category);

    StoredUpload Save(Stream content, string originalFileName, string? contentType, DocumentCategory category);

    string GetPhysicalPath(string relativePath);

    void TryDeletePhysical(string relativePath);
}

public sealed class FileStorage : IFileStorage
{
    private static readonly Dictionary<string, string> DocumentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    // Section 8 of the scope keeps the paper workflow: the completed form is uploaded as a scanned PDF or image.
    private static readonly Dictionary<string, string> ScanTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png"
    };

    private readonly string _root;
    private readonly SettingsCache _settings;
    private readonly long _configuredMaxBytes;

    public long MaxUploadBytes => _settings.MaxUploadBytes ?? _configuredMaxBytes;

    public FileStorage(IWebHostEnvironment environment, IConfiguration configuration, SettingsCache settings)
    {
        var configured = configuration["Storage:DocumentRootPath"] ?? "App_Data/documents";
        _root = Path.IsPathRooted(configured)
            ? configured
            : Path.GetFullPath(Path.Combine(environment.ContentRootPath, configured));

        Directory.CreateDirectory(_root);

        _settings = settings;
        var max = configuration.GetValue<long?>("Storage:MaxUploadBytes");
        _configuredMaxBytes = max is > 0 ? max.Value : 10 * 1024 * 1024;
    }

    public void Validate(string originalFileName, string? contentType, long sizeBytes, DocumentCategory category)
    {
        if (sizeBytes == 0)
        {
            throw new DomainException("The selected file is empty.");
        }

        if (sizeBytes > MaxUploadBytes)
        {
            throw new DomainException($"The file is larger than the allowed {MaxUploadBytes / (1024 * 1024)} MB limit.");
        }

        AllowedTypeFor(originalFileName, contentType, category);
    }

    public StoredUpload Save(Stream content, string originalFileName, string? contentType, DocumentCategory category)
    {
        var fileName = SanitizeFileName(originalFileName);
        var extension = Path.GetExtension(fileName);
        var expectedType = AllowedTypeFor(fileName, contentType, category);

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        EnsureMagicBytes(content, extension);

        var bucket = $"{CategoryFolder(category)}/{DateTime.UtcNow:yyyyMM}";
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = $"{bucket}/{storedName}";
        var physicalPath = GetPhysicalPath(relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);

        var target = new FileStream(physicalPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        try
        {
            using (target)
            {
                if (content.CanSeek)
                {
                    content.Position = 0;
                }

                content.CopyTo(target);
                return new StoredUpload(relativePath, fileName, expectedType, target.Length);
            }
        }
        catch
        {
            TryDeletePhysical(relativePath);
            throw;
        }
    }

    public string GetPhysicalPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new InvalidUploadException("The stored file reference is empty.");
        }

        var candidate = Path.GetFullPath(Path.Combine(_root, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        if (!candidate.StartsWith(Path.GetFullPath(_root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidUploadException("The stored file reference is not valid.");
        }

        return candidate;
    }

    public void TryDeletePhysical(string relativePath)
    {
        try
        {
            var path = GetPhysicalPath(relativePath);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (InvalidUploadException)
        {
        }
    }

    private static string AllowedTypeFor(string originalFileName, string? contentType, DocumentCategory category)
    {
        var allowed = category == DocumentCategory.CompletedEvaluationForm ? ScanTypes : DocumentTypes;
        var extension = Path.GetExtension(SanitizeFileName(originalFileName));

        if (!allowed.TryGetValue(extension, out var expectedType))
        {
            throw new InvalidUploadException(
                category == DocumentCategory.CompletedEvaluationForm
                    ? "Only PDF, JPG and PNG files are allowed for a scanned evaluation form."
                    : "Only PDF, DOC and DOCX files are allowed.");
        }

        if (!string.IsNullOrWhiteSpace(contentType) &&
            !string.Equals(contentType, expectedType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidUploadException($"The file claims to be {contentType}, which does not match a {extension} document.");
        }

        return expectedType;
    }

    private static string CategoryFolder(DocumentCategory category) => category switch
    {
        DocumentCategory.Cv => "cv",
        DocumentCategory.CompletedEvaluationForm => "evaluation",
        _ => "other"
    };

    private static string SanitizeFileName(string originalFileName)
    {
        var name = Path.GetFileName(originalFileName ?? string.Empty).Trim();

        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalid, '_');
        }

        name = name.Replace(' ', '_');

        if (string.IsNullOrWhiteSpace(Path.GetFileNameWithoutExtension(name)))
        {
            name = "document" + Path.GetExtension(name);
        }

        return name.Length <= 200 ? name : name[^200..];
    }

    private static void EnsureMagicBytes(Stream content, string extension)
    {
        Span<byte> header = stackalloc byte[4];
        var read = content.ReadAtLeast(header, 4, throwOnEndOfStream: false);

        ReadOnlySpan<byte> expected = extension switch
        {
            ".pdf" => "%PDF"u8,
            ".docx" => [0x50, 0x4B, 0x03, 0x04],
            ".doc" => [0xD0, 0xCF, 0x11, 0xE0],
            ".png" => [0x89, 0x50, 0x4E, 0x47],
            _ => default
        };

        if (extension is ".jpg" or ".jpeg")
        {
            if (read < 3 || header[0] != 0xFF || header[1] != 0xD8 || header[2] != 0xFF)
            {
                throw new InvalidUploadException("The file content does not look like a JPEG image.");
            }

            return;
        }

        if (read < 4 || !header.SequenceEqual(expected))
        {
            throw new InvalidUploadException($"The file content does not look like a {extension} document.");
        }
    }
}

public sealed class InvalidUploadException(string message) : Exception(message);
