using System.Security.Cryptography;
using System.Text;
using Auran.Clinic.Application.Auditing;
using Auran.Clinic.Application.Authentication;
using Auran.Clinic.Application.Files;
using Auran.Clinic.Domain.Entities;
using Auran.Clinic.Domain.Enums;
using Auran.Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Auran.Clinic.Infrastructure.Files;

public sealed class FileService(
    AuranClinicDbContext dbContext,
    ICurrentActor currentActor,
    IAuditService auditService,
    IFileStorageProvider storageProvider,
    IOptions<FileStorageOptions> fileStorageOptions,
    IHttpContextAccessor httpContextAccessor) : IFileService
{
    private readonly FileStorageOptions options = fileStorageOptions.Value;

    public async Task<FileUploadSessionResponse?> CreateUploadSessionAsync(
        CreateFileUploadSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetClinicActor(out var clinicId, out var userId))
            return null;
        if (request.Size > options.MaxFileSizeBytes)
            throw new InvalidOperationException($"File size exceeds the configured limit of {options.MaxFileSizeMb} MB.");
        if (options.Provider != storageProvider.Provider)
            throw new InvalidOperationException($"File storage provider '{options.Provider}' is not configured.");

        var originalName = Path.GetFileName(request.FileName.Trim());
        if (string.IsNullOrWhiteSpace(originalName))
            throw new InvalidOperationException("A valid file name is required.");

        var extension = Path.GetExtension(originalName).ToLowerInvariant();
        if (extension.Length > 20)
            throw new InvalidOperationException("File extension is too long.");

        var sessionId = Guid.NewGuid();
        var uploadToken = GenerateToken();
        var now = DateTime.UtcNow;
        var storageKey = BuildStorageKey(clinicId, sessionId, extension, now);
        var session = new FileUploadSession
        {
            Id = sessionId,
            ClinicId = clinicId,
            RequestedByUserId = userId,
            OriginalName = originalName,
            FileExtension = extension,
            ContentType = request.ContentType.Trim(),
            ExpectedSize = request.Size,
            StorageProvider = storageProvider.Provider,
            StorageKey = storageKey,
            UploadTokenHash = HashToken(uploadToken),
            Status = FileUploadStatus.Pending,
            ExpiresAtUtc = now.AddMinutes(Math.Clamp(options.UploadSessionMinutes, 1, 120))
        };

        dbContext.Set<FileUploadSession>().Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new FileUploadSessionResponse
        {
            SessionId = session.Id,
            UploadUrl = BuildUploadUrl(session.Id, uploadToken),
            ExpiresAtUtc = session.ExpiresAtUtc,
            StorageProvider = session.StorageProvider,
            ExpectedSize = session.ExpectedSize,
            UploadToken = uploadToken
        };
    }

    public async Task<FileUploadContentResult> UploadContentAsync(
        Guid sessionId,
        string uploadToken,
        Stream content,
        long? contentLength,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        var session = await dbContext.Set<FileUploadSession>()
            .SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (session is null || !TokenMatches(session.UploadTokenHash, uploadToken))
            return Failure("Upload session is invalid.");

        var now = DateTime.UtcNow;
        if (session.ExpiresAtUtc <= now)
        {
            session.Status = FileUploadStatus.Expired;
            await dbContext.SaveChangesAsync(cancellationToken);
            return Failure("Upload session has expired.");
        }
        if (session.Status is FileUploadStatus.Completed or FileUploadStatus.Expired or FileUploadStatus.Failed)
            return Failure("Upload session is no longer active.");
        if (session.StorageProvider != storageProvider.Provider)
            return Failure("Upload session storage provider is not available.");
        if (contentLength.HasValue && contentLength.Value != session.ExpectedSize)
            return Failure("Uploaded content length does not match the expected file size.");
        if (!string.IsNullOrWhiteSpace(contentType)
            && !contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase)
            && !contentType.Equals(session.ContentType, StringComparison.OrdinalIgnoreCase))
        {
            return Failure("Uploaded content type does not match the upload session.");
        }

        try
        {
            await storageProvider.SaveAsync(
                session.StorageKey,
                content,
                Math.Min(session.ExpectedSize, options.MaxFileSizeBytes),
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Failure(ex.Message);
        }

        var storedSize = await storageProvider.GetSizeAsync(session.StorageKey, cancellationToken);
        if (storedSize != session.ExpectedSize)
        {
            await storageProvider.DeleteIfExistsAsync(session.StorageKey, cancellationToken);
            return Failure("Uploaded file size does not match the expected file size.");
        }

        session.Status = FileUploadStatus.Uploaded;
        session.UploadedAtUtc = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new FileUploadContentResult { Succeeded = true };
    }

    public async Task<FileResponse?> CompleteUploadAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetClinicActor(out var clinicId, out var userId))
            return null;

        var session = await dbContext.Set<FileUploadSession>()
            .SingleOrDefaultAsync(
                x => x.Id == sessionId && x.ClinicId == clinicId && x.RequestedByUserId == userId,
                cancellationToken);
        if (session is null)
            return null;

        if (session.Status == FileUploadStatus.Completed && session.FileId.HasValue)
        {
            var completedFile = await dbContext.Files.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == session.FileId.Value && x.ClinicId == clinicId, cancellationToken);
            return completedFile is null ? null : Map(completedFile);
        }

        var now = DateTime.UtcNow;
        if (session.ExpiresAtUtc <= now)
        {
            session.Status = FileUploadStatus.Expired;
            await dbContext.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Upload session has expired.");
        }
        if (session.Status != FileUploadStatus.Uploaded)
            throw new InvalidOperationException("File content must be uploaded before completing the session.");

        var storedSize = await storageProvider.GetSizeAsync(session.StorageKey, cancellationToken);
        if (storedSize != session.ExpectedSize)
        {
            session.Status = FileUploadStatus.Failed;
            await dbContext.SaveChangesAsync(cancellationToken);
            await storageProvider.DeleteIfExistsAsync(session.StorageKey, cancellationToken);
            throw new InvalidOperationException("Stored file size does not match the upload session.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var file = new FileRecord
            {
                Id = Guid.NewGuid(),
                ClinicId = clinicId,
                OriginalName = session.OriginalName,
                StoredName = Path.GetFileName(session.StorageKey),
                FileExtension = session.FileExtension,
                ContentType = session.ContentType,
                Size = session.ExpectedSize,
                StorageProvider = session.StorageProvider,
                StorageKey = session.StorageKey,
                UploadedAtUtc = session.UploadedAtUtc ?? now,
                UploadedByUserId = userId
            };
            dbContext.Files.Add(file);

            session.Status = FileUploadStatus.Completed;
            session.CompletedAtUtc = now;
            session.FileId = file.Id;
            await dbContext.SaveChangesAsync(cancellationToken);

            await auditService.WriteAsync(new AuditEvent
            {
                Scope = AuditScope.Clinic,
                ClinicId = clinicId,
                Action = "File.UploadCompleted",
                Category = "File",
                EntityType = nameof(FileRecord),
                EntityId = file.Id.ToString(),
                Description = "File upload session completed and a permanent file record was created.",
                Metadata = new { file.OriginalName, file.FileExtension, file.ContentType, file.Size, file.StorageProvider }
            }, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return Map(file);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<FileResponse?> GetAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        if (!TryGetClinicActor(out var clinicId, out _))
            return null;

        var file = await dbContext.Files.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == fileId && x.ClinicId == clinicId, cancellationToken);
        return file is null ? null : Map(file);
    }

    public async Task<FileDownloadResponse?> OpenReadAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        if (!TryGetClinicActor(out var clinicId, out _))
            return null;

        var file = await dbContext.Files.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == fileId && x.ClinicId == clinicId, cancellationToken);
        if (file is null || file.StorageProvider != storageProvider.Provider)
            return null;

        var stream = await storageProvider.OpenReadAsync(file.StorageKey, cancellationToken);
        if (stream is null)
            return null;

        await auditService.WriteAsync(new AuditEvent
        {
            Scope = AuditScope.Clinic,
            ClinicId = clinicId,
            Action = "File.Downloaded",
            Category = "File",
            EntityType = nameof(FileRecord),
            EntityId = file.Id.ToString(),
            Description = "File content was downloaded.",
            Metadata = new { file.OriginalName, file.ContentType, file.Size }
        }, cancellationToken);

        return new FileDownloadResponse
        {
            Content = stream,
            ContentType = file.ContentType,
            FileName = file.OriginalName
        };
    }

    private FileResponse Map(FileRecord file) => new()
    {
        Id = file.Id,
        FileName = file.OriginalName,
        FileExtension = file.FileExtension,
        ContentType = file.ContentType,
        Size = file.Size,
        StorageProvider = file.StorageProvider,
        Url = BuildFileUrl(file.Id),
        UploadedAtUtc = file.UploadedAtUtc
    };

    private bool TryGetClinicActor(out Guid clinicId, out Guid userId)
    {
        clinicId = currentActor.ClinicId ?? Guid.Empty;
        userId = currentActor.ClinicUserId ?? Guid.Empty;
        return currentActor.IsAuthenticated
            && currentActor.ActorType == ActorType.Clinic
            && clinicId != Guid.Empty
            && userId != Guid.Empty;
    }

    private string BuildUploadUrl(Guid sessionId, string uploadToken)
    {
        var relative = $"/api/files/upload-sessions/{sessionId}/content?token={uploadToken}";
        return ToAbsoluteUrl(relative);
    }

    private string BuildFileUrl(Guid fileId) => ToAbsoluteUrl($"/api/files/{fileId}/content");

    private string ToAbsoluteUrl(string relative)
    {
        var request = httpContextAccessor.HttpContext?.Request;
        if (request is null)
            return relative;
        return $"{request.Scheme}://{request.Host}{request.PathBase}{relative}";
    }

    private static string BuildStorageKey(Guid clinicId, Guid sessionId, string extension, DateTime now) =>
        $"{clinicId:N}/{now:yyyy}/{now:MM}/{sessionId:N}{extension}";

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static bool TokenMatches(string expectedHash, string suppliedToken)
    {
        if (string.IsNullOrWhiteSpace(suppliedToken))
            return false;

        try
        {
            var expected = Convert.FromHexString(expectedHash);
            var actual = SHA256.HashData(Encoding.UTF8.GetBytes(suppliedToken.Trim()));
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static FileUploadContentResult Failure(string error) => new()
    {
        Succeeded = false,
        Error = error
    };
}
