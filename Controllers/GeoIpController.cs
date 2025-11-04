using GeoIpApi.Dtos;
using GeoIpApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace GeoIpApi.Controllers
{
    [ApiController]
    [Route("api/geoip")]
    public class GeoIpController : ControllerBase
    {
        private readonly IGeoIpClient _geoIpClient;

        public GeoIpController(IGeoIpClient geoIpClient) => _geoIpClient = geoIpClient;

        // GET /api/geoip/{ip}
        [HttpGet("{ip}")]
        [ProducesResponseType(typeof(GeoIpResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSingle([FromRoute] string ip, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(ip))
                return BadRequest("IP is required.");

            if (!IPAddress.TryParse(ip, out _))
                return BadRequest("Invalid IP address format.");

            try
            {
                var result = await _geoIpClient.LookUpAsync(ip, ct);
                return Ok(result);
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    $"FreeGeoIP unavailable or rate-limited: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}