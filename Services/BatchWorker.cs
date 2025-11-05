using GeoIpApi.Data;
using Microsoft.EntityFrameworkCore;

namespace GeoIpApi.Services
{
    public class BatchWorker : BackgroundService
    {
        // Logs worker activity (start, stop, errors).
        private readonly ILogger<BatchWorker> _logger;

        // Creates independent DI scopes for each job(so each job gets a new GeoDbContext).
        private readonly IServiceScopeFactory _scopeFactory;

        // The shared task queue(where jobs are waiting).
        private readonly IBgTaskQueue _bgTaskQueue;
        private readonly IGeoIpClient _geoIpClient;

        // Limits concurrency — here it allows max 5 parallel jobs at a time.
        private readonly SemaphoreSlim _throttle = new(5);

        public BatchWorker(ILogger<BatchWorker> logger,
                           IServiceScopeFactory scopeFactory,
                           IBgTaskQueue bgTaskQueue,
                           IGeoIpClient geoIpClient)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _bgTaskQueue = bgTaskQueue;
            _geoIpClient = geoIpClient;
        }

        // It runs continuously from the app start until the app shuts down.
        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            _logger.LogInformation("BatchWorker started.");

            // Tracks running tasks.
            var tasks = new List<Task>();
            try
            {
                // Wait for new tasks and process them as they come.
                while (!ct.IsCancellationRequested)
                {
                    // Wait for a new task to arrive and block until then.
                    var work = await _bgTaskQueue.DequeueAsync(ct);

                    // Throttle concurrency by ensuring we don't exceed max parallel tasks.
                    await _throttle.WaitAsync(ct);

                    // Start the job in the background.
                    var t = Task.Run(async () =>
                    {
                        try { await work(ct); }
                        finally { _throttle.Release(); }
                    }, ct);

                    // Task tracking and cleanup.
                    tasks.Add(t);
                    tasks.RemoveAll(t => t.IsCompleted);

                }
            }
            catch (OperationCanceledException) {

            }
        }

        // Puts all pending items of a batch into the background task queue.
        public static async Task EnqueueBatchItemsAsync(
            IServiceScopeFactory scopeFactory,
            IBgTaskQueue bgTaskQueue,
            IGeoIpClient geoIpClient,
            Guid batchId)
        {
            // Creates a temporasry scope to fetch pending item IDs.
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GeoDbContext>();

            // Fetch all pending item IDs for the given batch.
            var itemIds = await db.BatchItems
                .Where(i => i.BatchId == batchId && i.Status == "Pending")
                .Select(i => i.Id)
                .ToListAsync();

            // Enqueue each item as a separate background task.
            foreach (var itemId in itemIds)
            {
                await bgTaskQueue.EnqueueAsync(async ct => await ProcessItemAsync(scopeFactory, geoIpClient, batchId, itemId, ct)); 
            }
        }

        private static async Task ProcessItemAsync(
            IServiceScopeFactory scopeFactory,
            IGeoIpClient geoIpClient,
            Guid batchId,
            Guid itemId,
            CancellationToken ct)
        {
            // Each task gets its own scope and DbContext.
            using var inner = scopeFactory.CreateScope();
            var ctx = inner.ServiceProvider.GetRequiredService<GeoDbContext>();

            // Fetch the item to process.
            var item = await ctx.BatchItems.Include(x => x.Batch).FirstAsync(x => x.Id == itemId, ct);

            // Process the item.
            item.Status = "InProgress";
            item.StartedAt = DateTime.UtcNow;
            await ctx.SaveChangesAsync(ct);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                // Perform the GeoIP lookup.
                var result = await geoIpClient.LookUpAsync(item.Ip, ct);

                item.CountryCode = result.CountryCode;
                item.CountryName = result.CountryName;
                item.TimeZone = result.TimeZone;
                item.Latitude = result.Latitude;
                item.Longitude = result.Longitude;

                item.Status = "Done";
                item.Error = null;
            }
            catch (Exception ex)
            {
                item.Status = "Failed";
                item.Error = ex.Message;
            }
            finally
            {
                // Update timing.
                sw.Stop();
                item.CompletedAt = DateTime.UtcNow;
                item.DurationInMs = sw.ElapsedMilliseconds;

                await ctx.SaveChangesAsync(ct);
            }
        }
    }
}