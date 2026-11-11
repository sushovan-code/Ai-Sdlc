namespace AiSdlc.Application.Files.DownloadProjectFile;

/// <summary>The content to stream back. The caller owns and disposes <see cref="Content"/>.</summary>
public sealed record FileDownload(Stream Content, string FileName, string ContentType);
