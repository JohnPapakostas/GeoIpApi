using System.Threading.Channels;

namespace GeoIpApi.Services
{
    public class BgTaskQueue : IBgTaskQueue
    {
        // A thread-safe producer-consumer queue built into.NET.
        private readonly Channel<Func<CancellationToken, Task>> _queue;

        // CreateBounded creates a queue with a maximum capacity.
        public BgTaskQueue(int capacity = 1000)
            => _queue = Channel.CreateBounded<Func<CancellationToken, Task>>(capacity);

        // ValueTask: A lighter alternative to Task for async methods that often complete synchronously (better performance).
        // Reads the next job from the queue.
        public ValueTask<Func<CancellationToken, Task>> DequeueAsync(CancellationToken ct) => _queue.Reader.ReadAsync(ct);

        // Writes a new task into the queue.
        public ValueTask EnqueueAsync(Func<CancellationToken, Task> workItem) => _queue.Writer.WriteAsync(workItem);
    }
}
