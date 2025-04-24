using FluentResults;

namespace DiscoWeb.Queues;

public interface ITaskQueue<T> where T : class
{
    Task<Task<Result>> EnqueueAsync(T task);
    Task<T> DequeueAsync(CancellationToken cancellationToken);
}