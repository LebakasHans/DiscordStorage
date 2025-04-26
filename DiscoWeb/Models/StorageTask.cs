using FluentResults;

namespace DiscoWeb.Models;

public abstract class StorageTask
{
    public required string Path { get; set; }
    public TaskCompletionSource<Result<object>> CompletionSource { get; set; } = new();
}