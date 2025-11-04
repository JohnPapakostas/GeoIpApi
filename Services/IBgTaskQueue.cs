namespace GeoIpApi.Services
{
    public interface IBgTaskQueue
    {
        ValueTask EnqueueAsync(Func<CancellationToken, Task> workItem);

        ValueTask <Func<CancellationToken, Task>> DequeueAsync(CancellationToken ct);
    }
}
