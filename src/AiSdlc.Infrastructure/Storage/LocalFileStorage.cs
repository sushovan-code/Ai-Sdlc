using AiSdlc.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace AiSdlc.Infrastructure.Storage;

/// <summary>Stores files on local disk under a root folder. Every path is checked to stay inside that root.</summary>
internal sealed class LocalFileStorage : IFileStorage
{
    private const int BufferSize = 81920;
    private readonly string _root;

    public LocalFileStorage(IOptions<StorageOptions> options)
    {
        _root = Path.GetFullPath(options.Value.RootPath);
        Directory.CreateDirectory(_root);
    }

    public async Task SaveAsync(string key, Stream content, CancellationToken cancellationToken)
    {
        var path = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // Write to a temporary name first so a crash never leaves a half-written file under the real key.
        var temporary = path + ".tmp";
        await using (var target = new FileStream(
                         temporary, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous))
        {
            await content.CopyToAsync(target, cancellationToken);
        }

        File.Move(temporary, path, overwrite: true);
    }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        var path = Resolve(key);
        Stream? stream = File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan)
            : null;
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        var path = Resolve(key);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    /// <summary>Maps a key to a path and refuses anything that would leave the root (for example <c>..</c> segments).</summary>
    private string Resolve(string key)
    {
        var full = Path.GetFullPath(Path.Combine(_root, key));
        var rootWithSeparator = _root.EndsWith(Path.DirectorySeparatorChar) ? _root : _root + Path.DirectorySeparatorChar;
        if (!full.StartsWith(rootWithSeparator, StringComparison.Ordinal))
        {
            throw new ArgumentException("The storage key points outside the storage root.", nameof(key));
        }

        return full;
    }
}
