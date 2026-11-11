using System.Collections.Concurrent;

namespace AiSdlc.Application.Analysis;

/// <summary>
/// Lets only one analysis be generated per project at a time, and stops approve or reject while a draft is being
/// written. A generation holds the project for the length of the model calls, and a decision made in that window
/// would be overwritten by a draft built on stale state. This is in-process, which fits the single-instance app;
/// running several instances needs a shared lock or a database concurrency token instead.
/// </summary>
public sealed class AnalysisGenerationLock
{
    private readonly ConcurrentDictionary<Guid, byte> _running = new();

    public bool IsRunning(Guid projectId) => _running.ContainsKey(projectId);

    /// <returns>A handle to dispose when done, or <c>null</c> when a generation is already running for the project.</returns>
    public IDisposable? TryEnter(Guid projectId) =>
        _running.TryAdd(projectId, 0) ? new Release(_running, projectId) : null;

    private sealed class Release(ConcurrentDictionary<Guid, byte> running, Guid projectId) : IDisposable
    {
        public void Dispose() => running.TryRemove(projectId, out _);
    }
}
