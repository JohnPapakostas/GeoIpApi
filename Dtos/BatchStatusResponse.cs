namespace GeoIpApi.Dtos
{
    public class BatchStatusResponse
    {
        public Guid BatchId { get; set; }
        public string Progress { get; set; } = default!; // "20/100"
        public int Processed { get; set; }
        public int Total { get; set; }
        public long? EtaSeconds { get; set; }
        public string Status { get; set; } = default!;
        public List<Item> Items { get; set; } = new();

        public class Item
        {
            public string Ip { get; set; } = default!;
            public string Status { get; set; } = default!;
            public string? CountryCode { get; set; }
            public string? CountryName { get; set; }
            public string? TimeZone { get; set; }
            public double? Latitude { get; set; }
            public double? Longitude { get; set; }
            public string? Error { get; set; }
        }
    }
}
