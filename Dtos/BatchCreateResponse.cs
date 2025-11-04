namespace GeoIpApi.Dtos
{
    public class BatchCreateResponse
    {
        public Guid BatchId { get; set; }
        public string StatusUrl { get; set; } = default!;
    }
}
