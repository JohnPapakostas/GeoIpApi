using System.ComponentModel.DataAnnotations;

namespace GeoIpApi.Dtos
{
    public class BatchCreateRequest
    {
        [Required]
        public List<string> Ips { get; set; } = [];
    }
}
