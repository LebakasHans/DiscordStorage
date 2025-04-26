using DiscoWeb.Models;
using FluentResults;
using System.Threading.Channels;

namespace DiscoWeb.Queues;

public class StorageTaskQueue : ITaskQueue<StorageTask>
{
    private readonly Channel<StorageTask> _queue = Channel.CreateUnbounded<StorageTask>();

    public async Task<Task<Result<object>>> EnqueueAsync(StorageTask task)
    {
        await _queue.Writer.WriteAsync(task);
        return task.CompletionSource.Task;
    }

    public async Task<StorageTask> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }

}