using GeoIpApi.Dtos;

namespace GeoIpApi.Services
{
    public interface IGeoIpClient
    {
        Task<GeoIpResponse> LookUpAsync(string ip, CancellationToken ct = default);
    }
}
