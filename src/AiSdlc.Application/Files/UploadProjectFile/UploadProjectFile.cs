using AiSdlc.Domain.Projects;

namespace AiSdlc.Application.Files.UploadProjectFile;

/// <summary>Upload a new version. The handler reads at most <see cref="FileRules.MaxFileBytes"/> plus one byte from <c>Content</c>.</summary>
public sealed record UploadProjectFile(Guid ProjectId, FileKind Kind, string FileName, Stream Content);
