using FluentResults;

namespace DiscoWeb.Models;

public abstract class StorageTask
{
    public required string Path { get; set; }
    public TaskCompletionSource<Result> CompletionSource { get; set; } = new();
}