using System.ComponentModel.DataAnnotations;

namespace GeoIpApi.Models
{
    public class Batch
    {
        // Use a global unique id instead of auto increment id for easier tracking across systems. 
        [Key] 
        public Guid Id { get; set; } = Guid.NewGuid(); 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Number of IPs submitted in the batch.
        public int Total { get; set; }

        // Each batch has multiple items.
        public ICollection<BatchItem> Items { get; set; } = new List<BatchItem>();
    }
}
