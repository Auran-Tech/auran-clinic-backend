namespace Auran.Clinic.Application.Files;

public sealed record StoredFileWriteResult(
    string Provider,
    string StorageKey,
    string StoredName,
    long Size);

public sealed record StoredFileReadResult(
    Stream Content,
    string ContentType,
    string OriginalName);
