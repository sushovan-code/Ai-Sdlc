namespace AiSdlc.Application.Abstractions;

/// <summary>Stores file content under server-generated keys. Keys are never built from user input.</summary>
public interface IFileStorage
{
    Task SaveAsync(string key, Stream content, CancellationToken cancellationToken);

    /// <summary>Opens the stored content for reading, or returns <c>null</c> when the key does not exist.</summary>
    Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken);

    Task DeleteAsync(string key, CancellationToken cancellationToken);
}
