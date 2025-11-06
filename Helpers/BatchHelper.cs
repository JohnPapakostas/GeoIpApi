using GeoIpApi.Models;
using static GeoIpApi.Dtos.BatchStatusResponse;

namespace GeoIpApi.Helpers
{
    public static class BatchHelper
    {
        // Calculate ETA based on average processing time per item.
        public static (int processedCount, long etaSeconds) GetBatchProgress(Batch batch, List<Item> items)
        {
            var processed = items.Where(i => i.Status != null && (i.Status == "Done" || i.Status == "Failed"));
            var processedCount = processed.Count();
            var remaining = batch.Total - processedCount;
            var avgMsPerItem = processed.Where(i => i.DurationInMs != null).Select(i => i.DurationInMs!.Value).Average();

            long etaSeconds = 0;
            if (remaining > 0)
            {
                var etaMs = (long)Math.Ceiling(avgMsPerItem) * remaining;
                etaSeconds = (long)Math.Ceiling(etaMs / 1000.0);
            }

            return (processedCount, etaSeconds);
        }
    }
}
