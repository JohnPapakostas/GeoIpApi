namespace GeoIpApi.Dtos
{
    public class GeoIpResponse
    {
        public string Ip { get; set; } = default!;
        public string CountryCode { get; set; } = default!;
        public string CountryName { get; set; } = default!;
        public string TimeZone { get; set; } = default!;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
