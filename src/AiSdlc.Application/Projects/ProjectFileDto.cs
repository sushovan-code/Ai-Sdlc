using AiSdlc.Domain.Projects;

namespace AiSdlc.Application.Projects;

public sealed record ProjectFileDto(
    Guid Id,
    FileKind Kind,
    int Version,
    string FileName,
    long SizeBytes,
    string Sha256,
    DateTimeOffset UploadedAtUtc,
    Guid UploadedById,
    string UploadedByName,
    bool IsLatest);
