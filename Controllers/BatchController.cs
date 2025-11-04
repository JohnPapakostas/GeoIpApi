using GeoIpApi.Data;
using GeoIpApi.Dtos;
using GeoIpApi.Models;
using GeoIpApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace GeoIpApi.Controllers
{
    [ApiController]
    [Route("api/geoip/batch")]
    public class BatchController : ControllerBase
    {
        private readonly GeoDbContext _db;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IBgTaskQueue _queue;
        private readonly IGeoIpClient _client;

        public BatchController(GeoDbContext db,
                               IServiceScopeFactory scopeFactory,
                               IBgTaskQueue queue,
                               IGeoIpClient client)
        {
            _db = db; _scopeFactory = scopeFactory; _queue = queue; _client = client;
        }

        // (2) POST create batch
        [HttpPost]
        [ProducesResponseType(typeof(BatchCreateResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] BatchCreateRequest req)
        {
            if (req?.Ips == null || req.Ips.Count == 0)
                return BadRequest("Provide at least one IP.");

            // Optional: basic filtering for invalid IPs (keep only valid)
            var validIps = req.Ips
                .Where(ip => !string.IsNullOrWhiteSpace(ip) && IPAddress.TryParse(ip.Trim(), out _))
                .Select(ip => ip.Trim())
                .ToList();

            if (validIps.Count == 0)
                return BadRequest("No valid IPs.");

            var batch = new Batch { Total = validIps.Count, Processed = 0, Status = "Queued" };
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

            // enqueue work (one item → one queued task)
            _ = BatchWorker.EnqueueBatchItemsAsync(_scopeFactory, _queue, _client, batch.Id);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var statusUrl = $"{baseUrl}/api/geoip/batch/{batch.Id}/status";

            return Ok(new BatchCreateResponse { BatchId = batch.Id, StatusUrl = statusUrl });
        }

        // (3) GET status
        [HttpGet("{batchId:guid}/status")]
        [ProducesResponseType(typeof(BatchStatusResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Status([FromRoute] Guid batchId)
        {
            var batch = await _db.Batches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == batchId);
            if (batch == null) return NotFound("Batch not found.");

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
                    Error = i.Error
                })
                .ToListAsync();

            long? etaSeconds = null;
            var remaining = batch.Total - batch.Processed;
            if (remaining > 0 && batch.AvgMsPerItem > 0)
                etaSeconds = (batch.AvgMsPerItem * remaining) / 1000;

            return Ok(new BatchStatusResponse
            {
                BatchId = batch.Id,
                Processed = batch.Processed,
                Total = batch.Total,
                EtaSeconds = etaSeconds,
                Status = batch.Status,
                Items = items
            });
        }
    }
}
