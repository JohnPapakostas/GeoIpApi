using System.ComponentModel.DataAnnotations;

namespace GeoIpApi.Models
{
    public class BatchItem
    {
        [Key] 
        public Guid Id { get; set; } = Guid.NewGuid();

        // Foreign key to the Batch
        public Guid BatchId { get; set; }
        public Batch Batch { get; set; } = default!;

        public string Ip { get; set; } = default!;

        // Overall status: Pending, InProgress, Done, Failed
        public string Status { get; set; } = "Pending"; 

        // Geo Data
        public string? CountryCode { get; set; }
        public string? CountryName { get; set; }
        public string? TimeZone { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Diagnostics
        public string? Error { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public long? DurationInMs { get; set; }
    }
}
