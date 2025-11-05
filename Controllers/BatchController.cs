using GeoIpApi.Data;
using GeoIpApi.Dtos;
using GeoIpApi.Helpers;
using GeoIpApi.Models;
using GeoIpApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using static GeoIpApi.Dtos.BatchStatusResponse;

namespace GeoIpApi.Controllers
{
    [ApiController]
    [Route("api/geoip/batch")]
    public class BatchController : ControllerBase
    {
        private readonly GeoDbContext _db;
        // Create service scopes for background tasks.
        private readonly IServiceScopeFactory _scopeFactory;
        // Queue for background tasks.
        private readonly IBgTaskQueue _bgTaskQueue;
        // GeoIP client for lookups.
        private readonly IGeoIpClient _geoIpClient;

        // Constructor with dependencies injected.
        public BatchController(GeoDbContext db,
                               IServiceScopeFactory scopeFactory,
                               IBgTaskQueue bgTaskQueue,
                               IGeoIpClient geoIpClient)
        {
            _db = db; 
            _scopeFactory = scopeFactory; 
            _bgTaskQueue = bgTaskQueue; 
            _geoIpClient = geoIpClient;
        }

        // Create a new batch process for multiple IP addresses.
        [HttpPost]
        [ProducesResponseType(typeof(BatchCreateResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] BatchCreateRequest req)
        {
            // Check for given at least one IP.
            if (req?.Ips == null || req.Ips.Count == 0)
                return BadRequest("Provide at least one IP.");

            // Filter and validate IPs.
            var validIps = req.Ips
                .Where(ip => !string.IsNullOrWhiteSpace(ip) && IPAddress.TryParse(ip.Trim(), out _))
                .Select(ip => ip.Trim())
                .ToList();

            if (validIps.Count == 0)
                return BadRequest("No valid IPs.");

            // Create and store the batch and its items.
            var batch = new Batch { Total = validIps.Count, Status = "Queued" };
            _db.Batches.Add(batch);

            foreach (var ip in validIps)
            {
                _db.BatchItems.Add(new BatchItem
                {
                    Batch = batch,
                    Ip = ip,
                    Status = "Pending"
                });
            }
            await _db.SaveChangesAsync();

            // Enqueue the batch items for processing.
            _ = BatchWorker.EnqueueBatchItemsAsync(_scopeFactory, _bgTaskQueue, _geoIpClient, batch.Id);

            // Return the batch ID and status URL.
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var statusUrl = $"{baseUrl}/api/geoip/batch/{batch.Id}/status";

            return Ok(new BatchCreateResponse { BatchId = batch.Id, StatusUrl = statusUrl });
        }

        /* Return progress information for a given batch:
           1) How many items are done,
           2) Ηow many items are pending,
           3) Estimated time to completion (ETA),
           4) Status of each item,
           5) Details for each item.
        */
        [HttpGet("{batchId:guid}/status")] // Add validation for GUID.
        [ProducesResponseType(typeof(BatchStatusResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Status([FromRoute] Guid batchId)
        {
            // Find the batch by ID.
            // We use asNoTracking for read-only queries to improve performance.
            var batch = await _db.Batches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == batchId);
            if (batch == null) return NotFound("Batch not found.");

            // Get all items for the batch.
            // Fetches all IP items belonging to the batch, including their result data and errors.
            var items = await _db.BatchItems
                .Where(i => i.BatchId == batchId)
                .AsNoTracking()
                .Select(i => new BatchStatusResponse.Item
                {
                    Ip = i.Ip,
                    Status = i.Status,
                    CountryCode = i.CountryCode,
                    CountryName = i.CountryName,
                    TimeZone = i.TimeZone,
                    Latitude = i.Latitude,
                    Longitude = i.Longitude,
                    Error = i.Error,
                    DurationInMs = i.DurationInMs
                })
                .ToListAsync();
            
            (int processedCount, long etaSeconds) = BatchHelper.GetBatchProgress(batch, items);
            

            // Return the batch status response.
            return Ok(new BatchStatusResponse
            {
                BatchId = batch.Id,
                Processed = processedCount,
                Total = batch.Total,
                Progress = $"{processedCount}/{batch.Total}",
                EtaSeconds = etaSeconds,
                Status = batch.Status,
                Items = items
            });
        }
    }
}
