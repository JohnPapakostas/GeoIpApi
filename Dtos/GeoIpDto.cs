namespace GeoIpApi.Dtos
{
    public record GeoIpDto(
        string ip,
        string country_code,
        string country_name,
        string time_zone,
        double latitude,
        double longitude
    );
}
