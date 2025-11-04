using GeoIpApi.Dtos;
using System.Text.Json;
using static System.Net.WebRequestMethods;

namespace GeoIpApi.Services
{
    public class GeoIpClient: IGeoIpClient
    {
        private readonly HttpClient _httpClient;
        
        public GeoIpClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<GeoIpResponse> LookUpAsync(string ip, CancellationToken ct = default)
        {
            // Dispose the response after using it
            using var resp = await _httpClient.GetAsync($"json/{ip}", ct);
            
            // Throw an exception if status code is not successful
            resp.EnsureSuccessStatusCode();

            //Reads the response body as a stream
            await using var stream = await resp.Content.ReadAsStreamAsync(ct);

            // Deserialize the JSON response to GeoIpDto
            var data = await JsonSerializer.DeserializeAsync<GeoIpDto>(
            stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            ct
            ) ?? throw new InvalidOperationException("Invalid response from FreeGeoIP.");

            return new GeoIpResponse
            {
                Ip = data.ip,
                CountryCode = data.country_code,
                CountryName = data.country_name,
                TimeZone = data.time_zone,
                Latitude = data.latitude,
                Longitude = data.longitude
            };

        }
    }
}
